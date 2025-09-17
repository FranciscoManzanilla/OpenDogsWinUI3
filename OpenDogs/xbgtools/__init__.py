# author: Volfin
# 1.00 - Skipped
# 1.01 - Initial Release
# 1.02 - validated to Blender version 2.70
# 1.03 - added in suppor for stride 40 meshes; fixed material matchup problem
# 1.04 - added kludge for high-poly heads
# 1.05 - added support for collision segmented meshes
# 1.06 - fixed bug in bonedict processing


bl_info = {
    "name": "Watch Dogs 2 XBG Importer",
    "author": "Volfin",
    "version": (1, 0, 6),
    "blender": (2, 7, 0),
    "location": "File > Import > xbg (Watch Dogs 2 Model)",
    "description": "Import WD2, io: mesh",
    "warning": "",
    "wiki_url": "",
    "tracker_url": "",
    "category": "Import-Export"}
    
if "bpy" in locals():
    import imp
    if "import_WD2" in locals():
        imp.reload(import_WD2)
    if "export_WD2" in locals():
        imp.reload(export_WD2)

import bpy

from bpy.props import (StringProperty,
                       BoolProperty,
                       FloatProperty,
                       EnumProperty,
                       )

from bpy_extras.io_utils import (ImportHelper,path_reference_mode)

  
class WD2ImportOperator(bpy.types.Operator, ImportHelper):
    bl_idname = "import_scene.watchdogs2"
    bl_label = "Watch Dogs 2 Importer(.xbg)"
    
    filename_ext = ".xbg"
    skip_blank=False;

    randomize_colors = BoolProperty(\
        name="Random Material Colors",\
        description="Assigns a random color to each material",\
        default=True,\
        )

    import_vertcolors = BoolProperty(\
        name="Import Vertex Colors",\
        description="Import Vertex Colors",\
        default=False,\
        )
    
    use_layers = BoolProperty(\
        name="Seperate Mesh Layers",\
        description="Place Meshes on seperate layers",\
        default=True,\
        )
    mesh_scale = bpy.props.FloatProperty(
        name="Scale Factor",
        description="Mesh Import Scale Factor",
        default=1.0,
    )

    filter_glob = StringProperty(default="*.xbg") # , options={'HIDDEN'}
    filepath = bpy.props.StringProperty(subtype="FILE_PATH")        
    path_mode = path_reference_mode

    def execute(self, context):
        import os, sys
        print("Import Execute called")
        cmd_folder = os.path.dirname(os.path.abspath(__file__))
        if cmd_folder not in sys.path:
            sys.path.insert(0, cmd_folder)

        import import_WD2
        result=import_WD2.import_WD2(self.filepath, bpy.context,self.randomize_colors,self.import_vertcolors,self.skip_blank,self.use_layers,self.mesh_scale)

        # force back off
        #self.skip_blank=False
        #self.use_layers=False
        
        if result is not None:
            self.report({'ERROR'},result)

        return {'FINISHED'}

    def invoke(self, context, event):

        print("Import Invoke called")
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

    def draw(self, context):
        layout = self.layout
        col = layout.column(align=True)
        col.label('Mesh Scale Factor')
        col.prop(self, "mesh_scale")
        row = layout.row(align=True)
        row.prop(self, "randomize_colors")
        row = layout.row(align=False)
        row.prop(self, "import_vertcolors")
        row = layout.row(align=True)
        row.prop(self, "use_layers")

#
# Registration
#
def menu_func_import(self, context):
    self.layout.operator(WD2ImportOperator.bl_idname, text="xbg(Watch Dogs 2 Model)(.xbg)",icon='PLUGIN')
   
def register():
    bpy.utils.register_module(__name__)
    bpy.types.INFO_MT_file_import.append(menu_func_import)
    
def unregister():
    bpy.utils.unregister_module(__name__)
    bpy.types.INFO_MT_file_import.remove(menu_func_import)
    
if __name__ == "__main__":
    register()