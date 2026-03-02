python -m venv .venv
source .venv/bin/activate
pip install numpy~=1.26.4
pip install torch==2.3.1 --index-url https://download.pytorch.org/whl/cpu
pip install torch-geometric==2.5.3
pip install pyg_lib torch_scatter torch_sparse torch_cluster torch_spline_conv -f https://data.pyg.org/whl/torch-2.3.0+cpu.html
pip install ase
pip install Flask