import bpy
import sys
import os

# Obtener los últimos 2 argumentos como entrada y salida
input_file = sys.argv[-2]
output_file = sys.argv[-1]

# Limpiar la escena
bpy.ops.wm.read_factory_settings(use_empty=True)

# Agregar ruta del importador
script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

# Importar tu script
import import_WD2

# Llamar al importador
import_WD2.import_WD2(
    file=input_file,
    context=bpy.context,
    randomize_colors=False,
    import_vertcolors=False,
    skip_blank=True,
    use_layers=False,
    mesh_scale=1.0
)

# Exportar a .obj
bpy.ops.export_scene.obj(
    filepath=output_file,
    use_selection=False,
    use_materials=True
)

print("Exportacin completada: " + output_file)
