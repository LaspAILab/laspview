"""
简单的串行推理
"""
from typing import Optional

import torch
import numpy as np
from ase import Atoms
from torch import nn
from torch_geometric.data import Data
from torch_geometric.data import InMemoryDataset
from torch_geometric.loader import DataLoader
from ase.data import atomic_numbers
from ase.calculators.calculator import Calculator
from ase.calculators.calculator import all_changes, all_properties
from ase.optimize import LBFGS
from ase.filters import UnitCellFilter


class PBCDataset(InMemoryDataset):
    def __init__(self, datalist):
        super().__init__()
        self.data, self.slices = InMemoryDataset.collate(datalist)


def get_loader(datalist):
    dataset = PBCDataset(datalist)
    loader = DataLoader(dataset, batch_size=256)
    return next(iter(loader))


#################################################calculate 处理数据
def gen_cartesian_prod(_x, _y, _z, device):
    _x, _y, _z = _x.item(), _y.item(), _z.item()

    x = torch.cat((torch.zeros(1), torch.arange(-1 * _x, 0), torch.arange(1, _x + 1)))
    y = torch.cat((torch.zeros(1), torch.arange(-1 * _y, 0), torch.arange(1, _y + 1)))
    z = torch.cat((torch.zeros(1), torch.arange(-1 * _z, 0), torch.arange(1, _z + 1)))
    cartesian_prod = torch.cartesian_prod(x, y, z).to(device)
    return cartesian_prod


def build_pbc(pos, cell, cutoff=5.0, device=None):  # 按照pos和晶胞 计算扩胞的基础数据
    inv_distances = torch.norm(cell.inverse().t(), dim=1)  # 轴的长度
    cell_xyz_repeats = torch.ceil(cutoff * inv_distances).long()
    cell_num = torch.prod(2 * cell_xyz_repeats + 1, dim=0)  # 每个晶胞扩胞总计次数
    ##############################################################
    _x, _y, _z = cell_xyz_repeats
    image = gen_cartesian_prod(_x, _y, _z, device).repeat_interleave(pos.shape[0], 0)
    shift = image @ cell
    pos_j = pos.repeat((cell_num, 1)) + shift  # j的坐标
    ##############################################################
    i_num = torch.tensor(pos.shape[0])
    j_num = torch.tensor(shift.shape[0])
    return image, shift, cell_num, i_num, j_num, pos_j


######################################################################################base cell class
class CellCalculator(Calculator):
    implemented_properties = ("energy", "forces", "stress", "magmoms")

    def __init__(self, model, device, **kwargs):
        super().__init__(**kwargs)
        self.device = device
        self.model = model

    @torch.no_grad()
    def build_data(self, atoms):
        _z = [atomic_numbers[element] for element in atoms.get_chemical_symbols()]
        z = torch.tensor(_z, dtype=torch.long, device=self.device)
        _pos = atoms.get_positions()
        pos = torch.tensor(_pos, dtype=torch.float32, device=self.device)
        _cell = np.array(atoms.get_cell())
        cell = torch.tensor(_cell, dtype=torch.float32, device=self.device)
        ######################################################################################
        inv_cell = torch.inverse(cell).to(self.device)
        frac_pos = torch.matmul(pos, inv_cell)
        pos = frac_pos @ cell
        image, shift, cell_num, i_num, j_num, pos_j = build_pbc(pos, cell, cutoff=5.0, device=self.device)
        #############################################################################frac_pos
        cell = cell.to(self.device)
        #############################################################################
        cell = cell.unsqueeze(0)
        data = Data(
            z=z,
            pos=pos,
            cell=cell,
            image=image,
            frac_pos=frac_pos,  # 分数坐标
            cell_num=cell_num,  # 扩胞次数
            i_num=i_num,  # 胞内原子数
            j_num=j_num,  # 总原子数
            pos_j=pos_j,
        )
        data = data.to(self.device)
        return data, _z, _pos, _cell

    def nn_calculate(self, data):
        data_batch = get_loader([data, ])
        data_batch = data_batch.to(self.device)
        strain = torch.zeros_like(data_batch.cell, requires_grad=True).to(self.device)
        batch_input = [
            1,
            data_batch.cell,
            strain,
            data_batch.cell_num,
            data_batch.z,
            data_batch.pos,
            data_batch.pos_j,
            data_batch.frac_pos,
            data_batch.i_num,
            data_batch.image,
            data_batch.j_num,
            data_batch.batch
        ]
        energy, force, stress = self.model(*batch_input)
        energy = energy.detach().cpu().item()
        force = (-1 * force).detach().cpu().numpy()
        stress = (stress.squeeze(0) / torch.det(data_batch.cell.squeeze(0))).detach().cpu().numpy()
        return energy, force, stress


#####################################################################固定晶胞
class FixCellCalculator(CellCalculator):
    def calculate(self, atoms, properties=all_properties, system_changes=all_changes):
        super().calculate(atoms=atoms, properties=properties, system_changes=system_changes)
        data, _, _, _ = self.build_data(atoms)
        energy, force, stress = self.nn_calculate(data)
        self.results.update(energy=energy, free_energy=energy, forces=force)


class D3FixCellCalculator(CellCalculator):
    def calculate(self, atoms, properties=all_properties, system_changes=all_changes):
        super().calculate(atoms=atoms, properties=properties, system_changes=system_changes)
        data, z, pos, cell = self.build_data(atoms)
        energy, force, stress = self.nn_calculate(data)
        self.results.update(energy=energy, free_energy=energy, forces=force)


###################################################################################优化晶胞
class OptimCellCalculator(CellCalculator):
    def calculate(self, atoms, properties=all_properties, system_changes=all_changes):
        super().calculate(atoms=atoms, properties=properties, system_changes=system_changes)
        data, _, _, _ = self.build_data(atoms)
        energy, force, stress = self.nn_calculate(data)
        self.results.update(energy=energy, free_energy=energy, forces=force, stress=stress)


class D3OptimCellCalculator(CellCalculator):
    def calculate(self, atoms, properties=all_properties, system_changes=all_changes):
        super().calculate(atoms=atoms, properties=properties, system_changes=system_changes)
        data, z, pos, cell = self.build_data(atoms)
        energy, force, stress = self.nn_calculate(data)
        self.results.update(energy=energy, free_energy=energy, forces=force, stress=stress)


#####################################################forward
def optim_forward(atoms: Atoms, model: nn.Module, device: torch.device, optim_cell: bool) -> Optional[Atoms]:
    if optim_cell:
        # NN优化20步
        calc = OptimCellCalculator(model, device)
        atoms.calc = calc
        ucf = UnitCellFilter(atoms)  # 包装原子对象
        # dyn = LBFGS(ucf, logfile=None)
        dyn = LBFGS(ucf)
        # NN优化20步
        dyn.run(fmax=5, steps=50)
        fmax = np.max(np.linalg.norm(atoms.get_forces(), axis=1))
        if fmax > 20:
            print("Fmax > 20, escape")
            return None
        # NN + D3优化
        calc = D3OptimCellCalculator(model, device)
        atoms.calc = calc
        ucf = UnitCellFilter(atoms)  # 可复用 UnitCellFilter
        dyn = LBFGS(ucf, logfile=None)  # 重新创建 LBFGS 实例
        dyn.run(fmax=0.1, steps=100)
    else:
        # NN优化20步
        calc = FixCellCalculator(model, device)
        atoms.calc = calc
        # dyn = LBFGS(atoms, logfile=None)
        dyn = LBFGS(atoms)
        dyn.run(fmax=5, steps=50)
        fmax = np.max(np.linalg.norm(atoms.get_forces(), axis=1))
        if fmax > 20:
            print("Fmax > 20, escape")
            return None
        # NN + D3
        calc = D3FixCellCalculator(model, device)
        atoms.calc = calc
        dyn = LBFGS(atoms, logfile=None)  # 重新创建 LBFGS 实例
        dyn.run(fmax=0.1, steps=100)

    return atoms


def run_energy(cif: str, optim_model: nn.Module, device: torch.device) -> float:
    import io
    from ase.io import read

    atoms = read(io.StringIO(cif), format="cif")
    assert optim_model is not None, "`optim_model` should have been initialized"
    calc = CellCalculator(optim_model, device)

    data, _, _, _ = calc.build_data(atoms)
    energy, _, _ = calc.nn_calculate(data)

    return energy


def run_optim(cif: str, optim_model: nn.Module, device: torch.device, optim_cell: bool = True) -> str:
    import io
    from ase.io import read, write

    atoms = read(io.StringIO(cif), format="cif")
    assert optim_model is not None, "`optim_model` should have been initialized"
    res = optim_forward(atoms, optim_model, device, optim_cell)
    assert res is not None, "Optimization should return a result"

    out = io.BytesIO()
    write(out, res, format="cif")
    return out.getvalue().decode()


if __name__ == "__main__":
    with open(r"C:\Users\x1398\Downloads\SiOH.cif", "r") as f:
        cif_text = f.read()

    model_ = torch.jit.load("batch_lasp_model.pth", map_location='cpu')
    res = run_optim(cif_text, model_, torch.device('cpu'))
    print(res)
