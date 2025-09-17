# author: Volfin

import bpy
import os
import sys
import mathutils
import math
import platform
import imp
import csv
import struct
from bpy_extras.io_utils import unpack_list
from struct import *
from mathutils import Vector, Matrix, Quaternion, Euler
import time
import random

matList = [];  # materials List
boneMapList = [];
LODList = [];
filehandle = 0

def to_blender_matrix(bl_mat):
    mat = mathutils.Matrix()
    for i, col in enumerate(bl_mat):
        for k, val in enumerate(col):
            mat[k][i] = val
    return mat

def import_mesh(context,randomize_colors,import_vertcolors,use_layers,mesh_scale,msh_count,inputMeshList):
    
    global model_name
    SWAP_YZ_MATRIX = mathutils.Matrix.Rotation(math.radians(0.0), 4, 'X')

    # lets just do this to be sure    
    if bpy.ops.object.mode_set.poll():
        bpy.ops.object.mode_set(mode='OBJECT')
    #else:
    #    print("mode_set() context is incorrect, current mode is", bpy.context.mode)

    #Block 1
    BH(1)
    alignPosition(4)
    
    # establish variables
    me = 0
    me_obj = 0

    vertex=[]
    normals=[]
    uvs=[]
    UV2=[]
    UV3=[]
    colors=[]
    colors2=[]
    bone_list=[]
    w_count=[]
    m_weights=[]
    g_indices=[]
    faces=[]
    face_idx=[]
    matZones=[]

    material_count = 0
    face_count = 0 # running total

    # mesh scale per axis
    x_scalefactor=1.0
    y_scalefactor=1.0
    z_scalefactor=1.0

    model_count=len(inputMeshList)

    # pre-calculate faces offset
    vertBlockSize = 0
    for m in range(model_count):
        blocksize = 1+(inputMeshList[m][16]-inputMeshList[m][15])
        vertBlockSize = vertBlockSize + (blocksize * inputMeshList[m][4])

    faceBlockStart = vertBlockSize + filehandle.tell()+4 #skip one dword
    print("faceBlockStart = "+str(faceBlockStart))

    vertBlockfOffset = filehandle.tell() # store current offset
    filehandle.seek(faceBlockStart,0) # SEEK_SET
            
    face_count = BI(1)[0] // 2 #(block size / 2)
    print("Faces offset:"+str(filehandle.tell()))
    print("face_count / 3:"+str(face_count//3))

    faceBlockStart = filehandle.tell()
    filehandle.seek(vertBlockfOffset,0) # SEEK_SET
    
    vertSum=0 # running count of vertices
    runningFaceCount = 0
    subtractIndex = 0
    print("Initialized Face Count, looping for "+str(model_count)+" times.")
    for m in range(model_count):

        filehandle.seek(vertBlockfOffset,0) # SEEK_SET
        print("vertBlockfOffset:"+str(filehandle.tell()));

        if vertSum == 0:
            print("-- Starting New Mesh.");
            # start new mesh
            model_name2=model_name+"-"+str(msh_count)+"-"+str(model_count)
            me = bpy.data.meshes.new(model_name2)
            me_obj = bpy.data.objects.new(model_name2,me)
            me_obj.select = True # makes selected

            #objects_ptr.extend([me_obj]) #store for later
            context.scene.objects.link(me_obj)
            context.scene.objects.active = me_obj # makes active

            # read in mesh data
            vertex=[]
            normals=[]
            uvs=[]
            UV2=[]
            UV3=[]
            UV4=[]
            colors=[]
            colors2=[]
            bone_list=[]
            w_count=[]
            m_weights=[]
            g_indices=[]
            faces=[]
            face_idx=[]
            matZones=[]            
            material_count = 0


        params=inputMeshList[m]

        vertStride = params[4]
        vertCount = 1+params[16]-params[15]
        faceCount = params[10]
        faceOffset = params[12]
        
        if material_count == 0:
            subtractIndex = faceOffset
            
        totalVertCount = params[14]
        matID = params[2]
        UVFlag = params[3]
        matCount = params[20]
        print("faceOffset:"+str(faceOffset))
        print("subtractIndex:"+str(subtractIndex))
        print("faceCount:"+str(faceCount))
        matEntry = { 'matID':matID, 'start':(faceOffset - subtractIndex)//3,'end':((faceOffset - subtractIndex)+faceCount)//3}
        print("matEntry:"+str(matEntry))
        #matEntry = { 'matID':matID, 'start':face_count//3,'end':(face_count+faceCount)//3}
        matZones.append(matEntry)
        material_count = material_count + 1
        
        print("num, vertStride, vertCount, faceCount/3, totalVertCount, matID, UVFlag, matCount")
        print(m,vertStride,vertCount,faceCount//3,totalVertCount,matID,UVFlag,matCount)
        
        for n in range(vertCount):

            tmp = Bh(8)
            divfactor=65536.0
            shiftfactor=0.5
            vertex.extend([(tmp[2]/32768.0,tmp[3]/32768.0,tmp[4]/32768.0)])
            uvs.extend([(tmp[6]/divfactor+shiftfactor, 1.0-(tmp[7]/divfactor+shiftfactor))])

            if vertStride == 40:
                tmp=Bh(12)
            elif vertStride == 36:
                tmp=Bh(10)
                UV2.extend([(tmp[0]/divfactor+shiftfactor, 1.0-(tmp[1]/divfactor+shiftfactor))])
                #normals.extend([tmp[8]/32768.0,tmp[9]/32768.0,tmp[10]/32768.0])
            elif vertStride == 32:
                tmp=Bh(8)
                UV2.extend([(tmp[0]/divfactor+shiftfactor, 1.0-(tmp[1]/divfactor+shiftfactor))])
                #normals.extend([tmp[8]/32768.0,tmp[9]/32768.0,tmp[10]/32768.0])
            elif vertStride == 28:
                tmp=Bh(6)
            elif vertStride == 24:
                tmp=Bh(4)
            elif vertStride == 20:
                tmp=Bh(2)
            else:
                print ("Unknown VertStride:"+str(vertStride))
                return

        vertSum+=vertCount
        runningFaceCount+=faceCount
        print("Added "+str(vertCount)+" to vertSum. Added "+str(faceCount)+" to runningFaceCount.")
        vertBlockfOffset = filehandle.tell() # store current offset
        print("-- Stored vertBlockfOffset:"+str(filehandle.tell()));
        
        if vertSum==totalVertCount:
            vertSum = 0
            print('--Closing off mesh. runningFaceCount/3:'+str(runningFaceCount//3))

            ####################################################################
                 
            print("vertex array length:"+str(len(vertex)))
            print("normal array length:"+str(len(normals)))
            print("weights array length:"+str(len(m_weights)))
            print("index array length:"+str(len(g_indices)))
            print("UV1 array length:"+str(len(uvs)))
            print("UV2 array length:"+str(len(UV2)))
            print("UV3 array length:"+str(len(UV3)))
            print("Colors array length:"+str(len(colors)))

            #########################################
            ## FACES
            #########################################
            
            filehandle.seek(faceBlockStart,0) # SEEK_SET

            print("-- faceBlockStart:"+str(filehandle.tell()));

            # face block  
            for x in range(0,runningFaceCount//3):
                tmp=BH(3)
                faces.extend([(tmp[0],tmp[1],tmp[2])])
                face_idx.extend([0]) #initialize for fill-in later

            print("faces array length:"+str(len(faces)))

            # set new faces offset
            faceBlockStart = filehandle.tell()
            
            try:
                me.from_pydata(vertex,[],faces)
                if len(normals) > 0:
                    me.vertices.foreach_set("normal",normals) # add normals
                else:
                    bpy.ops.object.mode_set(mode='EDIT')
                    bpy.ops.mesh.select_all(action='SELECT')
                    bpy.ops.mesh.normals_make_consistent(inside=False)
                    bpy.ops.object.editmode_toggle()
            except:
                pass

            # set to smooth shaded	
            bpy.ops.object.shade_smooth()

            if use_layers:
                # place mesh on incremental layer, wrap back around if more than 20 layers
                
                me_obj.layers[msh_count%20] = True

                #wipe other layers
                for i in range(20):
                    # if there's ever more than 20 meshes, we are in trouble. But i doubt that happens.
                    me_obj.layers[i] = (i == msh_count)
            
            ###############################################
            ## Material Data
            ###############################################
            #material_count = 0 #BI(1)[0]

            # for now, skip past materials, will deal with later.
            if material_count > 0:
                for x in range(0,material_count): # add slots
                    bpy.ops.object.material_slot_add()
                    
                if matZones[material_count-1].get('end') < (face_count//3):
                    # we have some unassociated faces, so make a default material for them.
                    mat = bpy.data.materials.new("Base")
                    bpy.ops.object.material_slot_add()
                    me_obj.material_slots[material_count].material = mat
                    for j in range(0,runningFaceCount//3): # assign to all initially
                        face_idx[j]=material_count
                        
                for x in range(0,material_count):
                    matEntry = matZones[x]
                    
                    start_index = matEntry.get('start')
                    end_offset = matEntry.get('end')
                    m_id = matEntry.get('matID')        

                    me_obj.material_slots[x].material = matList[m_id]
                    print("setting materials slot ",x," to ",matList[m_id])
                    print("START:"+str(start_index)+" END:"+str(end_offset)+" m_id:"+str(m_id))

                    for j in range(start_index,end_offset): # Start number, count
                        #print("assigning material ",x," to index ",j)
                        face_idx[j]=x

                    if randomize_colors:
                        matList[m_id].diffuse_color = randomColor()
                    #mat.specular_color = material.specular()
                    #mat.mirror_color = material.emissive()
                    #mat.alpha = material.transparency_factor()
                    #mat.specular_intensity = material.shininess()
                
                    # texture
                    #for k, texture in enumerate(material.texture_list()):    
                    #    try:
                    #        tex = mat.texture_slots.create(k)
                    #        tex.texture = bpy.data.textures.new(texture.name(), type='IMAGE')
                    #        tex.texture_coords = 'UV'
                    #        tex.use = True
                    #        tex.use_map_color_diffuse = True
                    #        fbx_base_path, fbx_file_name = os.path.split(file)
                    #        fbx_texture_file_path = texture.file_name()
                    #        fbx_garbage_path, fbx_texture_name = os.path.split(fbx_texture_file_path)
                    #        base_norm = os.path.normpath(fbx_base_path)
                    #        garbage_norm = os.path.normpath(fbx_garbage_path)
                    #        if base_norm in garbage_norm and os.path.exists(fbx_texture_file_path):
                    #            texture_file_path = fbx_texture_file_path
                    #        else:
                    #            texture_file_path = bpy.path.abspath(os.path.join(fbx_base_path, fbx_texture_name))
                    #        texture_file_path = os.path.normpath(texture_file_path)
                    #        if os.path.exists(texture_file_path):
                    #            tex.texture.image = bpy.data.images.load(texture_file_path)
                    #    except:
                    #        pass

                    # read in some (as of now) unknown values

         
            # material index
            #print("Face_IDX:"+str(face_idx))
            me.polygons.foreach_set("material_index",face_idx) # should set multiple mat indexes.
            
            ###############################################
            if runningFaceCount > 0: # skip all this if no mesh

            ###############################################
            # Do UVs
            ###############################################
                if len(uvs) != 0:
                    me.uv_textures.new(name='UV_0')
                    uv_data = me.uv_layers[0].data
                    for i in range(len(uv_data)):
                        uv_data[i].uv = uvs[me.loops[i].vertex_index]
                if len(UV2) != 0:
                    me.uv_textures.new(name='UV_1')
                    uv_data = me.uv_layers[1].data
                    for i in range(len(uv_data)):
                        uv_data[i].uv = UV2[me.loops[i].vertex_index]
                if len(UV3) != 0:
                    me.uv_textures.new(name='UV_2')
                    uv_data = me.uv_layers[2].data
                    for i in range(len(uv_data)):
                        uv_data[i].uv = UV3[me.loops[i].vertex_index]
                if len(UV4) != 0:
                    me.uv_textures.new(name='UV_3')
                    uv_data = me.uv_layers[3].data
                    for i in range(len(uv_data)):
                        uv_data[i].uv = UV4[me.loops[i].vertex_index]

            ###############################################
            # Do Vertex Color
            ###############################################

                if len(colors) != 0 and import_vertcolors:
                    me.vertex_colors.new(name='Color_Data')
                    color_data = me.vertex_colors[0].data
                    for i in range(len(color_data)):
                        color_data[i].color = colors[me.loops[i].vertex_index]
                if len(colors2) != 0 and import_vertcolors:
                    me.vertex_colors.new(name='Color_Data2')
                    color_data = me.vertex_colors2[0].data
                    for i in range(len(color_data)):
                        color_data[i].color = colors2[me.loops[i].vertex_index]


            ###############################################
            # Finalize
            ###############################################

                # finalize mesh
                me.update(calc_edges=True)

                #scale
                scale_matrix = mathutils.Matrix.Scale(1.0*mesh_scale, 4)

                #scale_matrix[0][0] = scale_matrix[0][0]*x_scalefactor
                #scale_matrix[1][1] = scale_matrix[1][1]*y_scalefactor
                #scale_matrix[2][2] = scale_matrix[2][2]*z_scalefactor
                #print("0:"+str(scale_matrix[0][0]))
                #print("1:"+str(scale_matrix[1][1]))
                #print("2:"+str(scale_matrix[2][2]))
                me_obj.dimensions.x = x_scalefactor
                me_obj.dimensions.y = y_scalefactor
                me_obj.dimensions.z = z_scalefactor
                print("0:"+str(x_scalefactor))
                print("1:"+str(y_scalefactor))
                print("2:"+str(z_scalefactor))

                #global_trans = to_blender_matrix(mesh.global_transform())
                me_obj.matrix_basis = SWAP_YZ_MATRIX * scale_matrix#* global_trans

                # apply transforms (rotation)
                bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)

                # finalize mesh
                me.update(calc_edges=True)
                
            subtractIndex = runningFaceCount
            runningFaceCount = 0
            print('set subtractIndex to runningFaceCount, reset runningFaceCount:'+str(subtractIndex))

    vertBlockfOffset = vertBlockfOffset + ((face_count*2) + 8) # 8 for pre-face data
    print("position now:"+str(filehandle.tell()))
    print("where we would go:"+str(vertBlockfOffset))
    filehandle.seek(vertBlockfOffset,0) # SEEK_SET
    #alignPosition(4)
    #vertBlockfOffset = filehandle.tell() # store current offset
    print("skipped past face data");
            
    print("Finished Model")

    return 0 # all fine

##################################################################

def createRig(name, origin, boneTable):

    # Create armature and object
    bpy.ops.object.add(
        type='ARMATURE', 
        enter_editmode=True,
        location=origin)
    ob = bpy.context.object
    ob.show_x_ray = True
    ob.data.draw_type = 'STICK'
    ob.name = name
    amt = ob.data
    amt.name = name+'Amt'
    amt.show_axes = False
 
    # Create bones
    bpy.ops.object.mode_set(mode='EDIT')
    for (bname, pname, vector) in boneTable:    
        bone = amt.edit_bones.new(bname)
        if pname:
            parent = amt.edit_bones[pname]
            bone.parent = parent
            bone.head = parent.tail
            bone.use_connect = False
            if vector[0]+vector[1]+vector[2] < 0.0001:
                vector = (0,0.0001,0) # try to prevent zero length bones
            (trans, rot, scale) = parent.matrix.decompose()
        else:
            bone.head = (0,0,0)
            rot = Matrix.Translation((0,0,0))	# identity matrix
        bone.tail = rot * Vector(vector) + bone.head
    bpy.ops.object.mode_set(mode='OBJECT')
    return ob

def poseRig(rig, poseTable,use_layers,mesh_scale):

    SWAP_YZ_MATRIX = mathutils.Matrix.Rotation(math.radians(90.0), 4, 'X')
    bpy.context.scene.objects.active = rig
    bpy.ops.object.mode_set(mode='POSE')
 
    for (bname, loc,angle) in poseTable:
        
        pbone = rig.pose.bones[bname]

        pbone.matrix_basis.identity()
        # Set rotation mode to Euler XYZ, easier to understand
        # than default quaternions
        pbone.rotation_mode = 'XYZ'
        # Documentation bug: Euler.rotate(angle,axis):
        # axis in ['x','y','z'] and not ['X','Y','Z']
        print("quat:",angle)
        quat = Quaternion((angle[3]*-1.0,angle[0]*-1.0,angle[1],angle[2])) # flip on axis, Negate axis, negate 'w'
        euler=quat.to_euler('XYZ')
        #pbone.rotation_euler.rotate_axis('X', euler[0])
        #pbone.rotation_euler.rotate_axis('Y', euler[2])
        #pbone.rotation_euler.rotate_axis('Z', euler[1])

        #########################
        pos = Vector([float(loc[0]), float(loc[1]), float(loc[2])])
        rot = Euler([float(euler[0]), float(euler[1]), float(euler[2])])
        
        kf = Matrix.Identity(4)
        kf = Matrix.Translation(pos) * rot.to_matrix().to_4x4()

        if pbone.parent:
            pbone.matrix = pbone.parent.matrix * kf
        else:
            pbone.matrix = kf
        
    bpy.ops.pose.armature_apply()

    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,size=2)
    bone_vis = bpy.context.active_object
    bone_vis.data.name = bone_vis.name = "HitmanDat_bone_vis"
    bone_vis.use_fake_user = True
    bpy.context.scene.objects.unlink(bone_vis) # don't want the user deleting this
    bpy.context.scene.objects.active = rig
    ###########################

    # Calculate armature dimensions...Blender should be doing this!
    maxs = [0,0,0]
    mins = [0,0,0]
    for bone in rig.data.bones:
        for i in range(3):
            maxs[i] = max(maxs[i],bone.head_local[i])
            mins[i] = min(mins[i],bone.head_local[i])

    dimensions = []

    for i in range(3):
        dimensions.append(maxs[i] - mins[i])
        
    length = max(0.001, (dimensions[0] + dimensions[1] + dimensions[2]) / 600) # very small indeed, but a custom bone is used for display

    # Apply spheres
    bpy.ops.object.mode_set(mode='EDIT')
    for (bname, loc,angle) in poseTable:
        bone=rig.data.edit_bones[bname]
        bone.tail = bone.head + (bone.tail - bone.head).normalized() * length # Resize loose bone tails based on armature size
        rig.pose.bones[bone.name].custom_shape = bone_vis # apply bone shape

    #pbone.rotation_quaternion = quat
    bpy.ops.object.mode_set(mode='OBJECT')

    #scale
    scale_matrix = mathutils.Matrix.Scale(1.0*mesh_scale, 4)

    global_trans = mathutils.Matrix.Translation((poseTable[0][1][0]*100.0,poseTable[0][1][1]*100.0,poseTable[0][1][2]*100.0))
    matrix_basis = SWAP_YZ_MATRIX * scale_matrix * global_trans
    rig.data.transform(matrix_basis)
    
    # apply transforms (rotation)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)

    
    #add armature modifier
    for cnt,ob_ptr in enumerate(objects_ptr):
        mod = ob_ptr.modifiers.new('RigModifier', 'ARMATURE')
        mod.object = rig
        mod.use_bone_envelopes = False
        mod.use_vertex_groups = True

        # place armature on each layer as well
        if use_layers:
            rig.layers[cnt] = True

def xbtParser(filename):
    global filehandle;
    holdfile = filehandle #save
    filehandle=open(filename,'rb')	
    BH(4)
    w=BI(1)[0]
    filehandle.seek(w)

    # get size
    back=filehandle.tell()
    filehandle.seek(0,2) #SEEK_END
    size=filehandle.tell()
    filehandle.seek(back,0) #SEEK_SET
    
    new=open(filename.replace('.xbt','.dds'),'wb')	
    new.write(filehandle.read(size-filehandle.tell()))
    new.close()
    filehandle.close()
    filehandle=holdfile # restore

def skipMess():
    global filehandle;
    count=BI(1)[0]	
    for i in range(count):		
        BI(2)
        Bf(7)	
        BI(4)
        BI(1)
        type=BI(1)[0]
        if type==2:
            count=BI(1)[0]
            count=BI(1)[0]
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(16)
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(1)
            BI(1)[0]
            count=BI(1)[0]
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(16)
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(7)
            count=BI(1)[0]
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(16)
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(6)
            count=BI(1)[0]	
            for m in range(count):
                cv=BI(1)[0]
                count=BI(1)[0]
                for m in range(count):
                    Bf(5)
            count=BI(1)[0]		
            for m in range(count):
                cv=BI(1)[0]
                count=BI(1)[0]	
                for m in range(count):
                    Bf(9)
            count=BI(1)[0]		
            for m in range(count):
                cv=BI(1)[0]
                count=BI(1)[0]	
                for m in range(count):
                    Bf(9)
            count=BI(1)[0]
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(4)
                Bf(4)
                
            count=BI(1)[0]
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(4)
                    
            count=BI(1)[0]
            count=BI(1)[0]
            count=BI(1)[0]
            count=BI(1)[0]
        else:
            chunk=BI(1)[0]
            count=BI(1)[0]#8
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(16)
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(4)	
                Bf(1)
            count=BI(1)[0]#7
            for m in range(count):
                hash=BI(1)[0]
                tmp = file_str(BI(1)[0])
                print("crap str:"+tmp)
                alignPosition(16)
                Bf(23)			
            count=BI(1)[0]#6
            for m in range(count):
                hash=BI(1)[0]
                file_str(BI(1)[0])
                alignPosition(16)
                Bf(23)
            chunk=BI(1)[0]
            count=BI(1)[0]#5
            for m in range(count):
                BH(6)
                Bf(4)
            chunk=BI(1)[0]
            count=BI(1)[0]#4
            for m in range(count):
                filehandle.seek(44,1) #SEEK_CUR			
            count=BI(1)[0]#4
            for m in range(count):
                hash=BI(1)[0]
                tmp = file_str(BI(1)[0])
                print("crap2 str:"+tmp)
                alignPosition(4)
                Bf(4)			
            count=BI(1)[0]#3
            for m in range(count):
                hash=BI(1)[0]
                tmp = file_str(BI(1)[0])
                print("crap3 str:"+tmp)
                alignPosition(4)
                print("crap3 offset:"+str(filehandle.tell()))
            count=BI(1)[0]#2
            for m in range(count):
                BH(3)
            alignPosition(4)
            print("4 offset:"+str(filehandle.tell()))
            chunk=BI(1)[0]
            count=BI(1)[0]#4
            for m in range(count):
                BH(3)
            chunk=BI(1)[0]
            alignPosition(4)
            print("5 offset:"+str(filehandle.tell()))

def MeshList():
    meshList=[]
    meshCount=BI(1)[0]
    print('meshCount:'+str(meshCount))
    
    for d in range(meshCount):
        Bf(10)
        params=BH(22)		
        BI(1)
        #print m,params

        for e in range(params[20]):
            BI(17)
            len = BI(1)[0]
            if len > 128 or len == 0:
                filehandle.seek(-72,1) # SEEK_CUR
                print("Jumped to offset:"+str(filehandle.tell()))
                continue
            name=file_str(len)
            print("Name-end offset:"+str(filehandle.tell()))
            print("LOD Matname:"+str(name))
            alignPosition(4)
            BH(2)
        meshList.append(params)
    print("exiting offset:"+str(filehandle.tell()))
    return meshList

##################################################################

def alignPosition(alignment):
    alignment = alignment - 1
    amount = ((filehandle.tell() + alignment) & ~alignment)
    if (amount - filehandle.tell()) == (alignment +1):
        amount = filehandle.tell()        
    filehandle.seek(amount, 0) # SEEK_SET

def randomColor():
    randomR = random.random()
    randomG = random.random()
    randomB = random.random()
    return (randomR, randomG, randomB)

def HalfToFloat(h):
    s = int((h >> 15) & 0x00000001)    # sign
    e = int((h >> 10) & 0x0000001f)    # exponent
    f = int(h & 0x000003ff)            # fraction

    if e == 0:
       if f == 0:
          return int(s << 31)
       else:
          while not (f & 0x00000400):
             f <<= 1
             e -= 1
          e += 1
          f &= ~0x00000400
          #print (s,e,f)
    elif e == 31:
       if f == 0:
          return int((s << 31) | 0x7f800000)
       else:
          return int((s << 31) | 0x7f800000 | (f << 13))

    e = e + (127 -15)
    f = f << 13

    return int((s << 31) | (e << 23) | f)


def file_str(long): 
   s=''
   for j in range(0,long): 
       lit =  unpack('c',filehandle.read(1))[0]
       #print("ord of "+str(lit)+" is "+str(ord(lit)))
       if ord(lit)!= 0:
           s+=lit.decode("utf-8")
       else:
           break;
   return s

def readcstr():
    buf = ''
    while True:
        b = unpack('c',filehandle.read(1))[0]
        #print("ord of "+str(b)+" is "+str(ord(b)))
        if b is None or ord(b) == 0:
            return buf
        else:
            buf+=b.decode("utf-8")

def BB(n): # Unsigned Char Default is < (little Endian)  > = Big-Endian
    array = [] 
    for id in range(n): 
        array.append(unpack('B', filehandle.read(1))[0])
    return array
def Bb(n): # Signed Char
    array = [] 
    for id in range(n): 
        array.append(unpack('b', filehandle.read(1))[0])
    return array
def BH(n): # Unsigned Short
    array = [] 
    for id in range(n): 
        array.append(unpack('H', filehandle.read(2))[0])
    return array
def Bh(n): # Signed Short
    array = [] 
    for id in range(n): 
        array.append(unpack('h', filehandle.read(2))[0])
    return array
def Bf(n): # Float
	array = [] 
	for id in range(n):
		nz=filehandle.read(4)
		#print("NZ:"+str(nz))
		array.append(unpack('f',nz )[0])
	return array
def BF(n): # HALF FLoat
    array = [] 
    for id in range(n):
        nz=filehandle.read(2)
        #print("NZ:"+str(nz))
        v = struct.unpack('H', nz)
        x = HalfToFloat(v[0])
        # hack to coerce int to float
        str = struct.pack('I',x)
        f=struct.unpack('f', str)
        array.append(f[0])
    return array
def Bi(n): # Signed Long Int
    array = [] 
    for id in range(n): 
        array.append(unpack('i', filehandle.read(4))[0])
    return array
def BI(n): # Unsigned Long Int
    array = [] 
    for id in range(n): 
        array.append(unpack('I', filehandle.read(4))[0])
    return array
def Bd(n): # Double
    array = [] 
    for id in range(n): 
        array.append(unpack('d', filehandle.read(8))[0])
    return array

def views():
    """ Returns the set of 3D views.
    """
    rtn = []
    for a in bpy.data.screens["Default"].areas:
        if a.type == 'VIEW_3D':
            rtn.append(a)
    return rtn

def import_WD2(file, context, randomize_colors,import_vertcolors,skip_blank,use_layers,mesh_scale):

    global filehandle,model_name,matList,boneMapList,LODList
    report = None

    time1 = time.time()
    print("start")

    matList = [];  # materials List
    boneMapList = [];
    LODList = [];

    workingpath=os.path.dirname(os.path.realpath(__file__))

     #adjust clipping mode
    view=views()
    print(view[0].spaces[0].type)  # should be VIEW_3D
    view[0].spaces[0].clip_end=10000

    # split off model name
    root, ext = os.path.splitext(file)    
    model_name=root.split("\\")[-1]
    print(model_name)

    ##################################
    try:
        filehandle = open(file, "rb")
    except:
        report="Error loading '"+model_name+".xbg'\n"
        return report

    #get key data about the mesh
    Magic = file_str(4)
    Ma_ver = BH(1)[0]
    Mi_ver = BH(1)[0]

    print("Magic:"+str(Magic))
    if Magic != 'MOEG': # should be reversed GEOM header.
        report=model_name+".xbg doesn't seem to be a model file."
        return report

    if Ma_ver != 0x89 or Mi_ver != 0x46:
        report=model_name+".xbg is wrong version (Watchdogs 1?)."
        return report

    # unselect all   
    for item in bpy.context.selectable_objects:   
        item.select = False  

    #read in junk
    BI(4)
    unk_Count = BI(1)[0]
    odd_flag = BI(1)[0] #more on this later

    # skip odd_flag table
    if odd_flag > 0:
        for m in range(odd_flag):
            loc_count=BI(1)[0]
            for n in range(loc_count):
                Bf(8)
                
    BI(19) # skip some floats and stuff
    
    lod_Count = BI(1)[0]
    print("LOD count:"+str(lod_Count))
    
    for i in range(0,lod_Count):
        BI(2)

    BI(3)
    mat_count = BI(1)[0]
    BI(1)

    tag_count = 0
    for m in range(mat_count):
        # get mat path
        hash=BI(1)[0]
        matFile=file_str(BI(1)[0])
        print("Material File:"+matFile)
        alignPosition(4)
        #get mat name
        hash3= BI(1)[0]
        matName = file_str(BI(1)[0])
        alignPosition(4)
        
        mat = bpy.data.materials.new(matName)
        matPath=file.split('graphics')[0]+matFile
        
        if os.path.exists(matPath):
            fileheld = filehandle # store model file handle
            filehandle=open(matPath,'rb')
            
            filehandle.seek(68,0) #SEEK_SET
            file_str(BI(1)[0])
            alignPosition(4) # byte alignment
            file_str(BI(1)[0])
            alignPosition(4)
            
            filehandle.seek(32,1) #SEEK_CUR
            for n in range(1):
                xbtPath=file.split('graphics')[0]+file_str(BI(1)[0])
                alignPosition(4)
                if os.path.exists(xbtPath)==True:
                    xbtParser(xbtPath)
                    ddsPath=xbtPath.replace('.xbt','.dds')
                    if n==0:
                        #mat.diffuse=ddsPath
                        tex = mat.texture_slots.create(n)
                        tex.texture = bpy.data.textures.new('Auto Texture', type='IMAGE')
                        tex.texture_coords = 'UV'
                        tex.use = True
                        tex.use_map_color_diffuse = True
                        tex.texture.image = bpy.data.images.load(ddsPath)
                    BI(2)
            filehandle.close()
            filehandle = fileheld # back to model file
        matList.append(mat)

        #sub-block
        tag_count=BI(1)[0]
        if tag_count == 0:
            BI(1)
        
    for n in range(tag_count): # Tags + TAG1
        BI(1) #hash
        file_str(BI(1)[0])#name
        alignPosition(4)
        BI(1) #id
            
    alignPosition(4)

    #Skeleton
    
    skeleton_count=BI(1)[0]  # Block4
    skele_name = 'skeleton' #default
    for m in range(skeleton_count):
        #BH(1) #word
        alignPosition(4)
        BI(1) #hash
        skele_name=file_str(BI(1)[0])#name

    # Skeleton part 1 B
    alignPosition(2) # yes, odd
    
    boneIDCount=BH(1)[0]
    pBoneFlag=BH(1)[0] # entry_size?
    if pBoneFlag != 0:
        if pBoneFlag == 1:
            boneMapList.append(BH(boneIDCount*9))
            print("pBoneFlag == 1")
        elif pBoneFlag == 2:
            boneMapList.append(BH(boneIDCount*10))
            print("pBoneFlag == 2")
        elif pBoneFlag == 8:
            boneMapList.append(BH(boneIDCount*8))
            print("pBoneFlag == 8")
        else:
            boneMapList.append(BH(boneIDCount*4))
            print("pBoneFlag Other")
    else:
        BH(4) # skip                


    #Skeleton part 2
    alignPosition(4)
    chunk=BI(1)[0]# or count ?	
    if chunk==1:
        boneCount=BI(1)[0]		
        for m in range(boneCount):
            BI(13)
            file_str(BI(1)[0])#name
            alignPosition(4)

    #matrix    
    print("Matrix offset:"+str(filehandle.tell()))
    BI(1)
    matrix_count=BI(1)[0]
    alignPosition(16) #new
    
    for m in range(matrix_count):
        Bf(16) #4x4 matrix

    #skip block
    print("SkipBlock offset:"+str(filehandle.tell()))
    chunk=BI(1)[0]# or count ?	
    if chunk != 0:
        #skipCount=BI(1)[0]		
        for m in range(chunk):
            BB(1)

    #next
    alignPosition(4) #new
    print("next offset:"+str(filehandle.tell()))
    #chunk=BI(1)[0]# or count ?
    #for m in range(chunk):
    #    BI(18)
    #    BI(1) #entry #
    #    BI(1) #hash
    #    name = file_str(BI(1)[0])              
    #    print("entry:"+str(name))#name
    #    alignPosition(16)

    #    Bf(16) #matrix
        
    #    BI(10)
    #    BI(74)

    skipMess()

    #unobserved
    print("next offset:"+str(filehandle.tell()))
    chunk=BI(1)[0]# or count ?
    for m in range(chunk):
        b=BH(4)
        Bf(1)
        if b[1] == 6:
            BI(3)

    print("Pre-MeshList offset:"+str(filehandle.tell()))
    #LOD processing
    for m in range(lod_Count):
        meshList=MeshList()
        LODList.append(meshList)


    Skip14b=BI(1)[0] # unknown

    if Skip14b > 0:
        filehandle.seek(0,2) #SEEK_END
        filesize = filehandle.tell()
        filehandle.seek(0,0) #SEEK_SET
        bigfile = filehandle.read()
        start_off = 0
        while True:
            found_off=bigfile.find(b'\xff\xff\xff\xff',start_off)
            if found_off < filesize / 2: # should be at least halfway through the file
                start_off = found_off+4
            else:
                break
            
        found_off = found_off -36 # we hope
        bigfile = None
        print("found offset:"+str(found_off))
        filehandle.seek(found_off,0) #SEEK_SET
    else:
        Skip14c=BI(1)[0]

        if Skip14c > 0:
            filehandle.seek(Skip14c*128,1) #SEEK_CUR
            alignPosition(16)
        
    d_Start = BI(1)[0] #LOD data start
    d_End = BI(1)[0] # LOD data end

    print("d_Start:"+str(d_Start)+" d_End:"+str(d_End)+" offset:"+str(filehandle.tell()))

    if d_Start == 0 and d_End == 0:
        d_End = 1
    # at model start
    ####################################
    for i in range(0,d_End): # d_End
        # do mesh and materials
        result=import_mesh(context,randomize_colors,import_vertcolors,use_layers,mesh_scale,i,LODList[i+d_Start])

        if result is None:
            report = "Mesh Output Failed."
        elif result == 1:
            report = "Vertice counts didn't match, aborting!"


    # should be at end of models, process last
    filehandle.close()
    
    #############################################
    ## Skeleton
    #############################################

    has_skeleton = True
    
    # take root filename and add .skel extension
    root = root + ".skel"
    try:
        filehandle = open(root, "rb")
    except:
        has_skeleton = False  

    if has_skeleton:
        print("HAS SKEL")
        #get key data about the mesh
        signature = BI(1)[0]
        if signature != MAGIC_SIG:
            report=model_name+".skel signature is invalid:"+str(signature)+"\n"
            return report
        
        filehandle.seek(0x18)
        bone_block_len = BI(1)[0]
        print("Bone block length:"+str(bone_block_len))

        name_start=filehandle.tell()
        filehandle.seek(bone_block_len,1) #SEEK_CUR
        
        print("ended at:"+str(filehandle.tell()))

        names_offset_len = BI(1)[0]
        
        bone_names=[] # names array

        for y in range(0,names_offset_len):
            tmp=BI(1)[0] # name offset
            offsets_start=filehandle.tell()
            filehandle.seek(name_start+tmp,0) # SEEK_SET
            bone_names.append(readcstr())
            print("name:",bone_names[y])
            filehandle.seek(offsets_start)

        
        # skip past hash list
        hashes_len = BI(1)[0]
        filehandle.seek(hashes_len*4,1) # 4 bytes per hash #SEEK_CUR

        # Now get bone parent index list
        parent_index_len = BI(1)[0]
        Bone_parents = []
        
        for y in range(0,parent_index_len):
            Bone_parents.append(Bi(1)[0])  # signed so root = -1

        #Now get location/rotation data and build skeleton table as we go
        bone_count = BI(1)[0]
        Bone_Table=[]
        Pose_Table=[]

        for y in range(0,bone_count):
            loc_n_rot = Bf(8)

            parent_name = None
            parent_id=Bone_parents[y]
            if parent_id != -1:
                parent_name = bone_names[parent_id]                

            Bone_Table.extend([(bone_names[y],parent_name,(loc_n_rot[0]*-1,loc_n_rot[1],loc_n_rot[2]))]) # Flip on X Axis
            Pose_Table.extend([(bone_names[y],(loc_n_rot[0]*-1,loc_n_rot[1],loc_n_rot[2]),(loc_n_rot[4],loc_n_rot[5],loc_n_rot[6],loc_n_rot[7]))])

        # build the skeleton
        rig = createRig('Skeleton', Vector((0,0,0)), Bone_Table)
        poseRig(rig, Pose_Table,use_layers,mesh_scale)            

        filehandle.close()

    print("time is ", time.time() - time1)
    return report
