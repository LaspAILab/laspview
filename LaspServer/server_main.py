from traceback import format_tb
from typing import Optional

import torch
from torch import nn
from flask import Flask, request
import pyg_lib, torch_scatter, torch_sparse, torch_cluster
import argparse

from serial_optim import run_optim, run_energy

app = Flask(__name__)

device = torch.device("cpu")
model: Optional[nn.Module] = None


@app.route('/optimize', methods=['POST'])
def optimize():
    cif_data = request.data.decode('utf-8')
    app.logger.info(f"Optimize begin")

    try:
        # Optimize atoms and lattice
        result = run_optim(cif_data, model, device, True)
        return result
    except Exception as e:
        error_str = f"Error: {repr(e)}\n{format_tb(e.__traceback__)}"
        app.logger.error(error_str)
        return "Error", 500


@app.route('/optimize_fixed', methods=['POST'])
def optimize_fixed():
    cif_data = request.data.decode('utf-8')
    app.logger.info(f"Optimize begin")

    try:
        # Optimize atoms with the lattice fixed
        result = run_optim(cif_data, model, device, False)
        return result
    except Exception as e:
        error_str = f"Error: {repr(e)}\n{format_tb(e.__traceback__)}"
        app.logger.error(error_str)
        return "Error", 500


@app.route('/energy', methods=['POST'])
def energy():
    cif_data = request.data.decode('utf-8')
    app.logger.info(f"Energy calculation begin")

    try:
        result = run_energy(cif_data, model, device)
        return str(result)
    except Exception as e:
        error_str = f"Error: {repr(e)}\n{format_tb(e.__traceback__)}"
        app.logger.error(error_str)
        return "Error", 500


# API to check whether server is up and running
@app.route('/ping', methods=['GET'])
def ping():
    app.logger.info(f"Ping begin")

    return "Pong", 200


def main():
    parser = argparse.ArgumentParser(description='Lasp NN optimizer HTTP Server')
    parser.add_argument('--port', type=int, default=5000, help='Port to run the server on')
    parser.add_argument('--model-path', type=str, default="batch_lasp_model.pth", help='Path to the model')
    parser.add_argument('--cpu', action='store_true', help='Force CPU usage even if CUDA is available')
    args = parser.parse_args()

    global device
    if args.cpu:
        device = torch.device("cpu")
    else:
        device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
    print(f"Running on device: `{device}`")

    global model
    model = torch.jit.load(args.model_path, map_location=device)
    if model is None:
        raise ValueError("`model` should have been initialized")

    app.run(host='0.0.0.0', port=args.port)


if __name__ == '__main__':
    main()
