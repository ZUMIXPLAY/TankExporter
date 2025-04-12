﻿Imports System.Windows
Imports System.Windows.Forms
Imports System.Drawing.Imaging
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Web
Imports Tao.OpenGl
Imports Tao.Platform.Windows
Imports Tao.FreeGlut
Imports Tao.FreeGlut.Glut
Imports Tao.DevIl
Imports Microsoft.VisualBasic.Strings
Imports Ionic.Zip
Imports System.Runtime.InteropServices
Imports System.Threading
Module modTextures
    Enum ATLAS_TYPE
        ATLAS_AM
        ATLAS_GBMT
        ATLAS_MAO
    End Enum
    Public AM_index_texture_list() As list_
    Public GBMT_index_texture_list() As list_
    Public MAO_index_texture_list() As list_
    Public BAKE_CAMO As Integer = 0
    Public Structure list_
        Public list() As image_info_
    End Structure
    Public Structure image_info_
        Public name As String
        Public texture_id As Integer
    End Structure

    Public textures() As textures_
    Public Structure textures_
        Public c_name As String
        Public c_id As Integer
        Public n_name As String
        Public n_id As Integer
        Public colorIdMap As String
        Public gmm_name As String
        Public gmm_id As Integer
        Public ao_name As String
        Public ao_id As Integer
        Public detail_id As Integer
        Public g_detailMap_id As Integer
        Public detail_name As String
        Public g_det_name As String
        Public g_det_id As Integer
        Public doubleSided As Boolean
        Public alphaRef As Integer
        Public alphaTestEnabled As Integer
        Public skinned As Boolean
    End Structure

    Dim mStream As MemoryStream
    Public Function get_fbx_texture(ByVal name As String)
        'frmMain.gl_stop = True

        Dim ext = Path.GetExtension(name)
        Dim username = Environment.UserName
        If name.Contains("Users") Then
            Dim a = name.Split("\")
            Dim os As String = ""
            For i = 0 To a.Length - 1
                If a(i).Contains("Users") Then
                    a(i + 1) = username
                    Exit For
                End If
            Next
            For i = 0 To a.Length - 2
                os += a(i) + "\"
            Next
            os += a(a.Length - 1)
            name = os
        End If

        Dim id As Integer = -1
        If ext.ToLower.Contains(".png") Then
            id = load_png_file(name)
        End If
        If ext.ToLower.Contains(".dds") Then
            id = LoadTextureDDS(name)
        End If
        If ext.ToLower.Contains(".jpg") Then
            id = load_jpg_file(name)
        End If
        'frmMain.gl_stop = False
        Return id
    End Function
    Public Sub export_primitive_fbx_textures()


        Dim ar = Path.GetFileNameWithoutExtension(frmMain.OpenFileDialog1.FileName).Split("~")
        Dim name As String = Path.GetFileName(ar(0))
        FBX_Texture_path = Path.GetDirectoryName(My.Settings.fbx_path) + "\" + name
        If Not IO.Directory.Exists(FBX_Texture_path) Then
            System.IO.Directory.CreateDirectory(FBX_Texture_path)
        End If
        Dim abs_name As String = ""
        frmMain.info_Label.Visible = True

        stop_updating = True

        For i = 1 To object_count
            _group(i).fbx_texture_id = 0
            _group(i).rendered = False
        Next

        For i = 1 To object_count
            Dim alpha_enabled = _group(i).alphaTest

            With _group(i)
                Dim idx = .g_atlas_indexs.x
                If _group(i).is_atlas_type = 1 And _group(i).fbx_texture_id = 0 Then
                    _group(i).fbx_texture_id = i
                    'Albeto
                    abs_name = FBX_Texture_path + "\" + "Atlas_AM_map_" + i.ToString
                    save_fbx_atlas_build_texture(i, abs_name, True, False)
                    'normal
                    'abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(GBMT_index_texture_list(i).list(idx).name)
                    'save_fbx_atlas_build_texture(i, abs_name, False, False)
                    If i < object_count And Not frmFBX.texture_per_model_cb.Checked Then
                        For z = i + 1 To object_count
                            If _group(z).is_atlas_type = 1 And _group(z).fbx_texture_id = 0 Then
                                If _group(z).AM_atlas = _group(i).AM_atlas Then
                                    _group(z).fbx_texture_id = _group(i).fbx_texture_id
                                    save_fbx_atlas_build_texture(z, abs_name, False, False)
                                    _group(z).rendered = True
                                    'normal
                                    'abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(GBMT_index_texture_list(i).list(idx).name)
                                    'save_fbx_atlas_build_texture(z, abs_name, False, False)
                                End If
                            End If
                        Next
                        save_fbx_atlas_build_texture(i, abs_name, False, True)

                    Else
                        If _group(i).rendered = False Then
                            save_fbx_atlas_build_texture(i, abs_name, False, True)
                            _group(i).rendered = True
                        End If
                    End If
                Else
                    If _group(i).fbx_texture_id = 0 Then
                        _group(i).fbx_texture_id = i
                        'normal
                        abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.normal_name)
                        If abs_name = FBX_Texture_path + "\" Then
                            abs_name += "empty_NM_" + i.ToString
                            .normal_name = "empty_NM_" + i.ToString
                        End If
                        save_fbx_texture(.normal_Id, abs_name, True, alpha_enabled, 0)
                        'Albeto
                        abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.color_name)
                        If abs_name = FBX_Texture_path + "\" Then
                            abs_name += "empty_AM_" + i.ToString
                            .color_name = "empty_AM_" + i.ToString
                        End If
                        save_fbx_texture(.color_Id, abs_name, False, alpha_enabled, 0)
                    End If
                End If
            End With
        Next
        '=======================================================================
        ' create normal map using a different shader
        For i = 1 To object_count
            _group(i).fbx_N_texture_id = 0
            _group(i).rendered = False
        Next

        For i = 1 To object_count

            With _group(i)
                Dim idx = .g_atlas_indexs.x
                If _group(i).is_atlas_type = 1 And _group(i).fbx_N_texture_id = 0 Then
                    _group(i).fbx_N_texture_id = i
                    'normal
                    abs_name = FBX_Texture_path + "\" + "Atlas_NM_map_" + i.ToString
                    save_fbx_atlas_build_normal_texture(i, abs_name, True, False)
                    If i < object_count And Not frmFBX.texture_per_model_cb.Checked Then
                        For z = i + 1 To object_count
                            If _group(z).is_atlas_type = 1 And _group(z).fbx_N_texture_id = 0 Then
                                If _group(z).AM_atlas = _group(i).AM_atlas Then
                                    _group(z).fbx_N_texture_id = _group(i).fbx_N_texture_id
                                    save_fbx_atlas_build_normal_texture(z, abs_name, False, False)
                                    _group(z).rendered = True
                                End If
                            End If
                        Next
                        save_fbx_atlas_build_normal_texture(i, abs_name, False, True)

                    Else
                        If _group(i).rendered = False Then
                            save_fbx_atlas_build_normal_texture(i, abs_name, False, True)
                            _group(i).rendered = True
                        End If
                    End If
                Else
                    'these textures are already created at this point but we need the ID for them.
                    _group(i).fbx_N_texture_id = i

                End If


            End With
        Next

        frmMain.pb2.Visible = False

        frmMain.info_Label.Visible = False
        stop_updating = False

    End Sub
    Public Sub export_fbx_textures(ByVal AC As Boolean, flipy As Byte)

        Dim ar() As String
        If PRIMITIVES_MODE Then
            ar = Path.GetFileNameWithoutExtension(frmMain.OpenFileDialog1.FileName).Split("~")
        Else
            ar = TANK_NAME.Split(":")

        End If
        If CRASH_MODE Then
            ar(0) = ar(0) + "_CRASH"
        End If
        Dim name As String = Path.GetFileName(ar(0))
        FBX_Texture_path = Path.GetDirectoryName(My.Settings.fbx_path) + "\" + name
        If Not IO.Directory.Exists(FBX_Texture_path) Then
            System.IO.Directory.CreateDirectory(FBX_Texture_path)
        End If
        Dim abs_name As String = ""
        G_Buffer.init()

        updateEvent.Reset()

        Threading.Thread.Sleep(100)
        If SELECTED_CAMO_BUTTON > 0 Then
            Dim rs = MsgBox("You have camouflage selected. Bake it in to Diffuse?", MsgBoxStyle.YesNo, "I need an answer..")
            If rs = DialogResult.Yes Then
                BAKE_CAMO = 1
            Else
                BAKE_CAMO = False
            End If

        End If
        For i = 0 To textures.Length - 2

            Dim alpha_enabled = textures(i).alphaTestEnabled

            With textures(i)
                'color
                abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.c_name)
                frmMain.info_Label.Visible = True

                If SELECTED_CAMO_BUTTON > 0 And Not .c_name.Contains("chassis") And Not .c_name.Contains("track") Then
                    Dim part As Integer = 0
                    For k = 1 To object_count
                        If Path.GetFileNameWithoutExtension(_group(k).color_name) = Path.GetFileNameWithoutExtension(.c_name) Then
                            part = k
                            Exit For
                        End If
                    Next

                    If BAKE_CAMO = 0 Then
                        save_fbx_texture(.c_id, abs_name, False, alpha_enabled, flipy)
                    End If
                    save_fbx_textureCamouflaged(.c_id, .ao_id, SELECTED_CAMO_BUTTON, abs_name, part)
                    If File.Exists(abs_name + ".png") Then
                        stop_updating = False
                        'Return ' we are not wasting time on this.
                    End If
                Else
                    save_fbx_texture(.c_id, abs_name, False, alpha_enabled, flipy)
                End If
                'normal
                abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.n_name)
                save_fbx_texture(.n_id, abs_name, True, alpha_enabled, flipy)
                'ao
                abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.ao_name)
                save_fbx_texture(.ao_id, abs_name, False, alpha_enabled, flipy)
                'gmm
                abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.gmm_name)
                save_fbx_texture(.gmm_id, abs_name, False, alpha_enabled, flipy)
                'detail
                abs_name = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(.detail_name)
                save_fbx_texture(.detail_id, abs_name, False, alpha_enabled, flipy)

            End With

        Next
        frmMain.pb2.Visible = False
        frmMain.info_Label.Visible = False
        Try
        Catch ex As Exception

        End Try
        updateEvent.Set()
        G_Buffer.init()

    End Sub

    Public Sub save_fbx_textureCamouflaged(ByVal color_id As Integer, ByVal ao_id As Integer, ByVal camo_id As Integer, ByVal save_path As String, ByVal part As Integer)
        If color_id = -1 Then Return
        frmMain.info_Label.Text = "Exporting : " + save_path + ".png"
        If File.Exists(save_path + ".png") Then ' stop saving exiting FBX textures.. It crashes 3DS Max
            Return
        End If
        frmMain.pb2.Visible = False
        frmMain.pb2.Location = New Point(0, 0)
        frmMain.pb2.BringToFront()
        Application.DoEvents()
        If Not (Wgl.wglMakeCurrent(pb2_hDC, pb2_hRC)) Then
            MessageBox.Show("Unable to make rendering context current")
            End
        End If
        Gl.glUseProgram(0)
        'frmMain.gl_stop = True
        'While gl_busy
        'End While
        Dim w, h As Integer
        Gl.glEnable(Gl.GL_TEXTURE_2D)
        Gl.glActiveTexture(Gl.GL_TEXTURE0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, color_id)
        Gl.glGetTexLevelParameteriv(Gl.GL_TEXTURE_2D, 0, Gl.GL_TEXTURE_WIDTH, w)
        Gl.glGetTexLevelParameteriv(Gl.GL_TEXTURE_2D, 0, Gl.GL_TEXTURE_HEIGHT, h)
        Dim p As New Point(0.0!, 0.0!)
        frmMain.pb2.Width = w
        frmMain.pb2.Height = h
        Gl.glViewport(0, 0, w, h)
        Gl.glMatrixMode(Gl.GL_PROJECTION) 'Select Projection
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glOrtho(0, w, -h, 0, -200.0, 100.0) 'Select Ortho Mode
        Gl.glMatrixMode(Gl.GL_MODELVIEW)    'Select Modelview Matrix
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glReadBuffer(Gl.GL_BACK)
        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL)
        Gl.glDisable(Gl.GL_CULL_FACE)
        Gl.glDisable(Gl.GL_DEPTH_TEST)
        Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE)
        Dim e = Gl.glGetError

        Gl.glUseProgram(shader_list.camoExporter_shader)
        Gl.glUniform1i(CE_camo_bake, BAKE_CAMO)
        Gl.glUniform1i(CE_camo_Map, 0)
        Gl.glUniform1i(CE_AO_Map, 1)
        Gl.glUniform1i(CE_AM_Map, 2)
        Gl.glUniform4f(CE_tile, _object(part).camo_tiling.x, _object(part).camo_tiling.y, _object(part).camo_tiling.z, _object(part).camo_tiling.w)
        Gl.glUniform4f(CE_camo_tile, bb_tank_tiling(SELECTED_CAMO_BUTTON).x, bb_tank_tiling(SELECTED_CAMO_BUTTON).y, bb_tank_tiling(SELECTED_CAMO_BUTTON).z, bb_tank_tiling(SELECTED_CAMO_BUTTON).w)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 2)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, color_id)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 1)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, ao_id)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, bb_camo_texture_ids(camo_id))

        Gl.glBegin(Gl.GL_QUADS)

        '  CW...
        '  1 ------ 2
        '  |        |
        '  |        |
        '  4 ------ 3
        '
        Gl.glTexCoord2f(0.0!, 0.0!)
        Gl.glVertex2f(p.X, p.Y)

        Gl.glTexCoord2f(1.0!, 0.0!)
        Gl.glVertex2f(p.X + w, p.Y)

        Gl.glTexCoord2f(1.0!, 1.0!)
        Gl.glVertex2f(p.X + w, p.Y - h)

        Gl.glTexCoord2f(0.0!, 1.0!)
        Gl.glVertex2f(p.X, p.Y - h)
        Gl.glEnd()
        Gl.glUseProgram(0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 1)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)

        Gl.glFinish()
        Dim tId As Integer = Il.ilGenImage
        Il.ilBindImage(tId)
        Il.ilTexImage(w, h, 0, 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE, Nothing)

        Gl.glReadPixels(0, 0, w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Il.ilGetData())
        Gl.glFinish()

        Gl.glFinish()
        If BAKE_CAMO = 0 Then
            Il.ilSave(Il.IL_PNG, save_path + "_CAMO.png") ' save to temp
        Else
            Il.ilSave(Il.IL_PNG, save_path + ".png") ' save to temp

        End If
        Gl.glDisable(Gl.GL_TEXTURE_2D)
        Gdi.SwapBuffers(pb2_hDC)
        Il.ilBindImage(0)
        Il.ilDeleteImage(tId)
        Application.DoEvents()

    End Sub

    Public Sub save_fbx_atlas_build_texture(ByVal id As Integer, ByVal save_path As String, ByVal new_image As Boolean, save As Boolean)
        If id = -1 Then Return
        frmMain.info_Label.Text = "Exporting : " + save_path + ".png"
        If File.Exists(save_path + ".png") Then ' stop saving exiting FBX textures.. It crashes 3DS Max if its being used.
            'Return
        End If

        frmMain.pb2.Visible = False
        frmMain.pb2.Location = New Point(0, 0)
        frmMain.pb2.BringToFront()
        Application.DoEvents()
        If Not (Wgl.wglMakeCurrent(pb2_hDC, pb2_hRC)) Then
            MessageBox.Show("Unable to make rendering context current")
            End
        End If
        Dim w, h As Integer
        w = 4096 : h = 4096
        '========= switch ============
        If save Then GoTo save_it
        Gl.glUseProgram(0)
        'frmMain.gl_stop = True
        'While gl_busy
        'End While
        Gl.glEnable(Gl.GL_TEXTURE_2D)


        Gl.glUseProgram(shader_list.textureBuilder_shader)
        Gl.glUniform1i(textureBuilder_atlasAM, 0)
        Gl.glUniform1i(textureBuilder_atlasBlend, 1)
        Gl.glUniform1i(textureBuilder_atlasDirt, 2)


        Gl.glUniform4f(textureBuilder_atlasSize, _group(id).g_atlas_size.x, _group(id).g_atlas_size.y, _group(id).g_atlas_size.z, _group(id).g_atlas_size.w)
        Gl.glUniform4f(textureBuilder_indexes, _group(id).g_atlas_indexs.x, _group(id).g_atlas_indexs.y, _group(id).g_atlas_indexs.z, _group(id).g_atlas_indexs.w)

        Gl.glUniform4f(textureBuilder_tint0, _group(id).g_tile0Tint.x, _group(id).g_tile0Tint.y, _group(id).g_tile0Tint.z, _group(id).g_tile0Tint.w)
        Gl.glUniform4f(textureBuilder_tint1, _group(id).g_tile1Tint.x, _group(id).g_tile1Tint.y, _group(id).g_tile1Tint.z, _group(id).g_tile1Tint.w)
        Gl.glUniform4f(textureBuilder_tint2, _group(id).g_tile2Tint.x, _group(id).g_tile2Tint.y, _group(id).g_tile2Tint.z, _group(id).g_tile2Tint.w)
        Gl.glUniform4f(textureBuilder_dirtColor, _group(id).g_dirtColor.x, _group(id).g_dirtColor.y, _group(id).g_dirtColor.z, _group(id).g_dirtColor.w)
        Gl.glUniform2f(textureBuilder_repeat, _group(id).x_repete, _group(id).y_repete)

        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, _group(id).AM_atlas)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 1)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, _group(id).ATLAS_BLEND_ID)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 2)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, _group(id).ATLAS_DIRT_ID)

        Dim p As New Point(0.0!, 0.0!)
        frmMain.pb2.Width = w
        frmMain.pb2.Height = h
        Gl.glViewport(0, 0, w, h)
        Gl.glMatrixMode(Gl.GL_PROJECTION) 'Select Projection
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glOrtho(0.0, 1.0, 0.0, 1.0, -200.0, 100.0) 'Select Ortho Mode
        '========= switch ============
        If new_image Then
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT)
        End If

        Gl.glMatrixMode(Gl.GL_MODELVIEW)    'Select Modelview Matrix
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glReadBuffer(Gl.GL_BACK)

        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL)

        Gl.glDisable(Gl.GL_CULL_FACE)
        Gl.glDisable(Gl.GL_DEPTH_TEST)
        Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE)
        Dim e = Gl.glGetError
        Dim uv1a, uv1b, uv1c As vec2
        Dim uv2a, uv2b, uv2c As vec2
        Dim p1, p2, p3 As Integer
        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE)
        Gl.glLineWidth(6)
        Gl.glBegin(Gl.GL_TRIANGLES)
        For j = 1 To _group(id).indices.Length - 1
            p1 = _group(id).indices(j).v1 - _group(id).startVertex_
            p2 = _group(id).indices(j).v2 - _group(id).startVertex_
            p3 = _group(id).indices(j).v3 - _group(id).startVertex_
            uv2a.x = _group(id).vertices(p1).u2
            uv2a.y = _group(id).vertices(p1).v2
            uv2b.x = _group(id).vertices(p2).u2
            uv2b.y = _group(id).vertices(p2).v2
            uv2c.x = _group(id).vertices(p3).u2
            uv2c.y = _group(id).vertices(p3).v2

            uv1a.x = _group(id).vertices(p1).u
            uv1a.y = _group(id).vertices(p1).v
            uv1b.x = _group(id).vertices(p2).u
            uv1b.y = _group(id).vertices(p2).v
            uv1c.x = _group(id).vertices(p3).u
            uv1c.y = _group(id).vertices(p3).v

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2a.x, uv2a.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1a.x, uv1a.y)
            Gl.glVertex2f(uv2a.x, uv2a.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2b.x, uv2b.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1b.x, uv1b.y)
            Gl.glVertex2f(uv2b.x, uv2b.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2c.x, uv2c.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1c.x, uv1c.y)
            Gl.glVertex2f(uv2c.x, uv2c.y)
        Next
        Gl.glEnd()
        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL)
        Gl.glBegin(Gl.GL_TRIANGLES)
        For j = 1 To _group(id).indices.Length - 1
            p1 = _group(id).indices(j).v1 - _group(id).startVertex_
            p2 = _group(id).indices(j).v2 - _group(id).startVertex_
            p3 = _group(id).indices(j).v3 - _group(id).startVertex_
            uv2a.x = _group(id).vertices(p1).u2
            uv2a.y = _group(id).vertices(p1).v2
            uv2b.x = _group(id).vertices(p2).u2
            uv2b.y = _group(id).vertices(p2).v2
            uv2c.x = _group(id).vertices(p3).u2
            uv2c.y = _group(id).vertices(p3).v2

            uv1a.x = _group(id).vertices(p1).u
            uv1a.y = _group(id).vertices(p1).v
            uv1b.x = _group(id).vertices(p2).u
            uv1b.y = _group(id).vertices(p2).v
            uv1c.x = _group(id).vertices(p3).u
            uv1c.y = _group(id).vertices(p3).v

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2a.x, uv2a.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1a.x, uv1a.y)
            Gl.glVertex2f(uv2a.x, uv2a.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2b.x, uv2b.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1b.x, uv1b.y)
            Gl.glVertex2f(uv2b.x, uv2b.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2c.x, uv2c.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1c.x, uv1c.y)
            Gl.glVertex2f(uv2c.x, uv2c.y)
        Next
        Gl.glEnd()
        Gl.glFinish()

        Gl.glUseProgram(0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 2)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 1)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)

save_it:
        '========= switch ============
        If Not save Then Return

        Dim tId As Integer = Il.ilGenImage
        Il.ilBindImage(tId)
        Il.ilTexImage(w, h, 0, 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE, Nothing)

        Gl.glReadPixels(0, 0, w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Il.ilGetData())
        Gl.glFinish()

        Gl.glFinish()
        Il.ilSave(Il.IL_PNG, save_path + ".png") ' save to temp
        Gl.glDisable(Gl.GL_TEXTURE_2D)
        Gdi.SwapBuffers(pb2_hDC)
        Il.ilBindImage(0)
        Il.ilDeleteImage(tId)
        Application.DoEvents()

    End Sub

    Public Sub save_fbx_atlas_build_normal_texture(ByVal id As Integer, ByVal save_path As String, ByVal new_image As Boolean, save As Boolean)
        If id = -1 Then Return
        frmMain.info_Label.Text = "Exporting : " + save_path + ".png"
        If File.Exists(save_path + ".png") Then ' stop saving exiting FBX textures.. It crashes 3DS Max if its being used.
            'Return
        End If

        frmMain.pb2.Visible = False
        frmMain.pb2.Location = New Point(0, 0)
        frmMain.pb2.BringToFront()
        Application.DoEvents()
        If Not (Wgl.wglMakeCurrent(pb2_hDC, pb2_hRC)) Then
            MessageBox.Show("Unable to make rendering context current")
            End
        End If
        Dim w, h As Integer
        w = 4096 : h = 4096
        '========= switch ============
        If save Then GoTo save_it
        Gl.glUseProgram(0)
        'frmMain.gl_stop = True
        'While gl_busy
        'End While
        Gl.glEnable(Gl.GL_TEXTURE_2D)


        Gl.glUseProgram(shader_list.textureNormalBuilder_shader)
        Gl.glUniform1i(textureNormalBuilder_atlasAM, 0)
        Gl.glUniform1i(textureNormalBuilder_atlasBlend, 1)


        Gl.glUniform4f(textureNormalBuilder_atlasSize, _group(id).g_atlas_size.x, _group(id).g_atlas_size.y, _group(id).g_atlas_size.z, _group(id).g_atlas_size.w)
        Gl.glUniform4f(textureNormalBuilder_indexes, _group(id).g_atlas_indexs.x, _group(id).g_atlas_indexs.y, _group(id).g_atlas_indexs.z, _group(id).g_atlas_indexs.w)

        Gl.glUniform2f(textureNormalBuilder_repeat, _group(id).x_repete, _group(id).y_repete)

        If frmFBX.convert_normal_maps_cb.Checked Then
            Gl.glUniform1i(textureNormalBuilder_convert, 1)
        Else
            Gl.glUniform1i(textureNormalBuilder_convert, 0)
        End If

        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, _group(id).GBMT_atlas)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 1)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, _group(id).ATLAS_BLEND_ID)

        Dim p As New Point(0.0!, 0.0!)
        frmMain.pb2.Width = w
        frmMain.pb2.Height = h
        Gl.glViewport(0, 0, w, h)
        Gl.glMatrixMode(Gl.GL_PROJECTION) 'Select Projection
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glOrtho(0.0, 1.0, 0.0, 1.0, -200.0, 100.0) 'Select Ortho Mode
        '========= switch ============
        If new_image Then
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT)
        End If

        Gl.glMatrixMode(Gl.GL_MODELVIEW)    'Select Modelview Matrix
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glReadBuffer(Gl.GL_BACK)


        Gl.glDisable(Gl.GL_CULL_FACE)
        Gl.glDisable(Gl.GL_DEPTH_TEST)
        Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE)
        Dim e = Gl.glGetError
        Dim uv1a, uv1b, uv1c As vec2
        Dim uv2a, uv2b, uv2c As vec2
        Dim p1, p2, p3 As Integer
        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE)
        Gl.glLineWidth(6)
        Gl.glBegin(Gl.GL_TRIANGLES)
        For j = 1 To _group(id).indices.Length - 1
            p1 = _group(id).indices(j).v1 - _group(id).startVertex_
            p2 = _group(id).indices(j).v2 - _group(id).startVertex_
            p3 = _group(id).indices(j).v3 - _group(id).startVertex_
            uv2a.x = _group(id).vertices(p1).u2
            uv2a.y = _group(id).vertices(p1).v2
            uv2b.x = _group(id).vertices(p2).u2
            uv2b.y = _group(id).vertices(p2).v2
            uv2c.x = _group(id).vertices(p3).u2
            uv2c.y = _group(id).vertices(p3).v2

            uv1a.x = _group(id).vertices(p1).u
            uv1a.y = _group(id).vertices(p1).v
            uv1b.x = _group(id).vertices(p2).u
            uv1b.y = _group(id).vertices(p2).v
            uv1c.x = _group(id).vertices(p3).u
            uv1c.y = _group(id).vertices(p3).v

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2a.x, uv2a.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1a.x, uv1a.y)
            Gl.glVertex2f(uv2a.x, uv2a.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2b.x, uv2b.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1b.x, uv1b.y)
            Gl.glVertex2f(uv2b.x, uv2b.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2c.x, uv2c.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1c.x, uv1c.y)
            Gl.glVertex2f(uv2c.x, uv2c.y)
        Next
        Gl.glEnd()
        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL)
        Gl.glBegin(Gl.GL_TRIANGLES)
        For j = 1 To _group(id).indices.Length - 1
            p1 = _group(id).indices(j).v1 - _group(id).startVertex_
            p2 = _group(id).indices(j).v2 - _group(id).startVertex_
            p3 = _group(id).indices(j).v3 - _group(id).startVertex_
            uv2a.x = _group(id).vertices(p1).u2
            uv2a.y = _group(id).vertices(p1).v2
            uv2b.x = _group(id).vertices(p2).u2
            uv2b.y = _group(id).vertices(p2).v2
            uv2c.x = _group(id).vertices(p3).u2
            uv2c.y = _group(id).vertices(p3).v2

            uv1a.x = _group(id).vertices(p1).u
            uv1a.y = _group(id).vertices(p1).v
            uv1b.x = _group(id).vertices(p2).u
            uv1b.y = _group(id).vertices(p2).v
            uv1c.x = _group(id).vertices(p3).u
            uv1c.y = _group(id).vertices(p3).v

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2a.x, uv2a.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1a.x, uv1a.y)
            Gl.glVertex2f(uv2a.x, uv2a.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2b.x, uv2b.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1b.x, uv1b.y)
            Gl.glVertex2f(uv2b.x, uv2b.y)

            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE1, uv2c.x, uv2c.y)
            Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, uv1c.x, uv1c.y)
            Gl.glVertex2f(uv2c.x, uv2c.y)
        Next
        Gl.glEnd()
        Gl.glFinish()

        Gl.glUseProgram(0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 1)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Gl.glActiveTexture(Gl.GL_TEXTURE0 + 0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)

        Gl.glLineWidth(1)

save_it:
        '========= switch ============
        If Not save Then Return

        Dim tId As Integer = Il.ilGenImage
        Il.ilBindImage(tId)
        Il.ilTexImage(w, h, 0, 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE, Nothing)

        Gl.glReadPixels(0, 0, w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Il.ilGetData())
        Gl.glFinish()

        Gl.glFinish()
        Il.ilSave(Il.IL_PNG, save_path + ".png") ' save to temp
        Gl.glDisable(Gl.GL_TEXTURE_2D)
        Gdi.SwapBuffers(pb2_hDC)
        Il.ilBindImage(0)
        Il.ilDeleteImage(tId)
        Application.DoEvents()

    End Sub


    Public Sub save_fbx_texture(ByVal id As Integer, ByVal save_path As String, ByVal n_map As Boolean, alpha_enabled As Integer, flipy As Byte)
        If id = -1 Then Return
        frmMain.info_Label.Text = "Exporting : " + save_path + ".png"
        If File.Exists(save_path + ".png") Then ' stop saving exiting FBX textures.. It crashes 3DS Max
            'Return
        End If
        frmMain.pb2.Visible = False
        frmMain.pb2.Location = New Point(0, 0)
        frmMain.pb2.BringToFront()
        Application.DoEvents()
        If Not (Wgl.wglMakeCurrent(pb2_hDC, pb2_hRC)) Then
            MessageBox.Show("Unable to make rendering context current")
            End
        End If
        Gl.glUseProgram(0)
        'frmMain.gl_stop = True
        'While gl_busy
        'End While
        Dim w, h As Integer
        Gl.glEnable(Gl.GL_TEXTURE_2D)
        Gl.glActiveTexture(Gl.GL_TEXTURE0)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, id)
        Gl.glGetTexLevelParameteriv(Gl.GL_TEXTURE_2D, 0, Gl.GL_TEXTURE_WIDTH, w)
        Gl.glGetTexLevelParameteriv(Gl.GL_TEXTURE_2D, 0, Gl.GL_TEXTURE_HEIGHT, h)
        Dim p As New Point(0.0!, 0.0!)
        frmMain.pb2.Width = w
        frmMain.pb2.Height = h
        Gl.glViewport(0, 0, w, h)
        Gl.glMatrixMode(Gl.GL_PROJECTION) 'Select Projection
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glOrtho(0, w, -h, 0, -200.0, 100.0) 'Select Ortho Mode
        Gl.glMatrixMode(Gl.GL_MODELVIEW)    'Select Modelview Matrix
        Gl.glLoadIdentity() 'Reset The Matrix
        Gl.glReadBuffer(Gl.GL_BACK)
        Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL)
        Gl.glDisable(Gl.GL_CULL_FACE)
        Gl.glDisable(Gl.GL_DEPTH_TEST)
        Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE)

        Gl.glUseProgram(shader_list.convertNormalMap_shader)
        Gl.glUniform1i(convertMap_flip_y, flipy)
        Gl.glUniform1i(convertMap_convert, 0)
        Gl.glUniform1i(convertMap_alpha_enabled, alpha_enabled)

        If n_map Then
            If frmFBX.convert_normal_maps_cb.Checked Then
                Gl.glUniform1i(convertMap_convert, 1)
            Else
                Gl.glUniform1i(convertMap_convert, 0)
            End If
            If frmFBX.flip_normal_cb.Checked Then
                Gl.glUniform1i(convertMap_flip_y, 1)
            Else
                Gl.glUniform1i(convertMap_flip_y, 0)
            End If
        End If


        Gl.glBegin(Gl.GL_QUADS)

        '  CW...
        '  1 ------ 2
        '  |        |
        '  |        |
        '  4 ------ 3
        '
        Gl.glTexCoord2f(0.0!, 0.0!)
        Gl.glVertex2f(p.X, p.Y)

        Gl.glTexCoord2f(1.0!, 0.0!)
        Gl.glVertex2f(p.X + w, p.Y)

        Gl.glTexCoord2f(1.0!, 1.0!)
        Gl.glVertex2f(p.X + w, p.Y - h)

        Gl.glTexCoord2f(0.0!, 1.0!)
        Gl.glVertex2f(p.X, p.Y - h)
        Gl.glEnd()

        Gl.glFinish()

        Gl.glUseProgram(0)

        Dim tId As Integer = Il.ilGenImage
        Il.ilBindImage(tId)
        Il.ilTexImage(w, h, 0, 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE, Nothing)

        Gl.glReadPixels(0, 0, w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Il.ilGetData())
        Gl.glFinish()

        Gl.glFinish()
        Il.ilSave(Il.IL_PNG, save_path + ".png") ' save to temp
        Gl.glDisable(Gl.GL_TEXTURE_2D)
        Gdi.SwapBuffers(pb2_hDC)
        Il.ilBindImage(0)
        Il.ilDeleteImage(tId)
        Application.DoEvents()

    End Sub

    Private Sub get_atlas_stuff(ByVal id As Integer)
        'This stops loading textures already loaded....
        '===========================================================================================
        'I assumed all atlases would use the same textures, blend and dirt.. NOT SO!
        'I'm making sure if they do dont use the same we load them!
        'color
        ReDim Preserve AM_index_texture_list(id)
        ReDim Preserve GBMT_index_texture_list(id)
        ReDim Preserve MAO_index_texture_list(id)

        AM_index_texture_list(id) = New list_
        GBMT_index_texture_list(id) = New list_
        MAO_index_texture_list(id) = New list_
        ReDim AM_index_texture_list(id).list(164)
        ReDim GBMT_index_texture_list(id).list(164)
        ReDim MAO_index_texture_list(id).list(164)

        If _group(id).is_atlas_type = 1 Then
            Dim diff_atlas_names As Boolean = True
            If id > 1 Then
                For k = id - 1 To 1 Step -1
                    'Loop and search for matching atlas name. if it matches, copy it or set flag to load new set.
                    'All other variables are loaded when the textures are searched for in ModTankLoader.vb
                    If _group(id).albedoHeightTile0 = _group(k).albedoHeightTile0 Then
                        If _group(k).is_atlas_type = 1 Then
                            diff_atlas_names = False
                            _group(id).AM_atlas = _group(k).AM_atlas
                            _group(id).image_size = _group(k).image_size

                            ReDim AM_index_texture_list(id).list(AM_index_texture_list(k).list.Length - 1)
                            For z = 0 To AM_index_texture_list(k).list.Length - 1
                                AM_index_texture_list(id).list(z) = New image_info_
                                AM_index_texture_list(id).list(z).name = AM_index_texture_list(k).list(z).name
                                AM_index_texture_list(id).list(z).texture_id = AM_index_texture_list(k).list(z).texture_id
                            Next
                            Exit For
                        End If
                    End If
                Next
            End If
            If diff_atlas_names Or id = 1 Then
                get_packed_atlas(_group(id).albedoHeightTile0, id, ATLAS_TYPE.ATLAS_AM)
            End If
        End If
        '===========================================================================================
        'GBMT
        If _group(id).is_atlas_type = 1 Then
            Dim diff_atlas_names As Boolean = True
            If id > 1 Then
                For k = id - 1 To 1 Step -1
                    If _group(id).normalGlossSpecTile0 = _group(k).normalGlossSpecTile0 Then
                        If _group(k).is_atlas_type = 1 Then
                            diff_atlas_names = False
                            _group(id).GBMT_atlas = _group(k).GBMT_atlas

                            ReDim GBMT_index_texture_list(id).list(GBMT_index_texture_list(k).list.Length - 1)
                            For z = 0 To GBMT_index_texture_list(k).list.Length - 1
                                GBMT_index_texture_list(id).list(z) = New image_info_
                                GBMT_index_texture_list(id).list(z).name = GBMT_index_texture_list(k).list(z).name
                                GBMT_index_texture_list(id).list(z).texture_id = GBMT_index_texture_list(k).list(z).texture_id
                            Next
                            Exit For
                        End If
                    End If
                Next
            End If
            If diff_atlas_names Or id = 1 Then
                get_packed_atlas(_group(id).normalGlossSpecTile0, id, ATLAS_TYPE.ATLAS_GBMT)
            End If
        End If
        '===========================================================================================
        'MAO
        If _group(id).is_atlas_type = 1 Then
            Dim diff_atlas_names As Boolean = True
            If id > 1 Then
                For k = id - 1 To 1 Step -1
                    If _group(id).metallicAOTile0 = _group(k).metallicAOTile0 Then
                        If _group(k).is_atlas_type = 1 Then
                            diff_atlas_names = False
                            _group(id).MAO_atlas = _group(k).MAO_atlas

                            ReDim MAO_index_texture_list(id).list(MAO_index_texture_list(k).list.Length - 1)
                            For z = 0 To GBMT_index_texture_list(k).list.Length - 1
                                MAO_index_texture_list(id).list(z) = New image_info_
                                MAO_index_texture_list(id).list(z).name = MAO_index_texture_list(k).list(z).name
                                MAO_index_texture_list(id).list(z).texture_id = MAO_index_texture_list(k).list(z).texture_id
                            Next
                            Exit For
                        End If
                    End If
                Next
            End If
            If diff_atlas_names Or id = 1 Then
                get_packed_atlas(_group(id).metallicAOTile0, id, ATLAS_TYPE.ATLAS_MAO)
            End If
        End If
        '===========================================================================================
        'BLEND
        If _group(id).is_atlas_type = 1 Then
            Dim diff_atlas_names As Boolean = True
            If id > 1 Then
                For k = id - 1 To 1 Step -1
                    If _group(id).blendMask = _group(k).blendMask Then
                        If _group(k).is_atlas_type = 1 Then
                            diff_atlas_names = False
                            _group(id).ATLAS_BLEND_ID = _group(k).ATLAS_BLEND_ID
                            Exit For
                        End If
                    End If
                Next
            End If
            If diff_atlas_names Or id = 1 Then

                _group(id).ATLAS_BLEND_ID = get_DDS_search_option(_group(id).blendMask.Replace(".png", "_hd.dds"))
                If _group(id).ATLAS_BLEND_ID = 0 Then
                    _group(id).ATLAS_BLEND_ID = get_DDS_search_option(_group(id).blendMask.Replace(".png", ".dds"))
                End If
                Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, worker_fbo.worker_fbo)
                worker_fbo.blur(_group(id).ATLAS_BLEND_ID)
                Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0)
            End If
        End If
        '===========================================================================================
        'DIRT
        If _group(id).is_atlas_type = 1 Then
            Dim diff_atlas_names As Boolean = True
            If id > 1 Then
                For k = id - 1 To 1 Step -1
                    If _group(id).dirtMap = _group(k).dirtMap Then
                        If _group(k).is_atlas_type = 1 Then
                            diff_atlas_names = False
                            _group(id).ATLAS_DIRT_ID = _group(k).ATLAS_DIRT_ID
                            _group(id).g_dirtColor = _group(k).g_dirtColor
                            Exit For
                        End If
                    End If
                Next
            End If
            If diff_atlas_names Or id = 1 Then
                If _group(id).dirtMap IsNot Nothing Then
                    _group(id).ATLAS_DIRT_ID = get_DDS_search_option(_group(id).dirtMap.Replace(".png", "_hd.dds"))
                    If _group(id).ATLAS_DIRT_ID = 0 Then
                        _group(id).ATLAS_DIRT_ID = get_DDS_search_option(_group(id).dirtMap.Replace(".png", ".dds"))
                    End If
                End If
                'Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, worker_fbo.worker_fbo)
                'worker_fbo.blur(_group(id).ATLAS_DIRT_ID)
                'Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0)

            End If
        End If

    End Sub


    Public Sub build_textures(ByVal id As Integer)
        Dim s = TheXML_String
        Try
            If PRIMITIVES_MODE Then 'only if loaded a stand alone
                get_atlas_stuff(id)
            End If
            If _group(id).is_atlas_type = 1 Then
                Return
            End If
            If _group(id).color_name Is Nothing Then
                Return
            End If
            'If PRIMITIVES_MODE Then Return
            Dim diffuse As String = _group(id).color_name.Replace(".dds", "_hd.dds")
            Dim normal As String = _group(id).normal_name.Replace(".dds", "_hd.dds")
            Dim metal As String = _group(id).GMM_name.Replace(".dds", "_hd.dds")

            Dim ao_name As String = ""
            If Not String.IsNullOrEmpty(_group(id).ao_name) Then
                ao_name = _group(id).ao_name.Replace(".dds", "_hd.dds")
            End If

            Dim colorIdMap As String = ""
            If Not String.IsNullOrEmpty(_group(id).colorIDmap) Then
                colorIdMap = _group(id).colorIDmap.Replace(".dds", "_hd.dds")
            End If
            Dim detail_name As String = _group(id).detail_name
            Dim g_det_name As String = ""
            If Not String.IsNullOrEmpty(_group(id).g_detailMap) Then
                g_det_name = _group(id).g_detailMap.Replace(".dds", "_hd.dds")
            End If
            'This stops loading textures already loaded....
            '===========================================================================================
            Dim i As Integer = 0
            For i = 0 To textures.Length - 1

                If textures(i).c_name = diffuse Then
                    _group(id).color_name = textures(i).c_name
                    _group(id).color_Id = textures(i).c_id
                    _group(id).normal_name = textures(i).n_name
                    _group(id).normal_Id = textures(i).n_id
                    _group(id).GMM_name = textures(i).gmm_name
                    _group(id).GMM_Id = textures(i).gmm_id
                    _group(id).ao_name = textures(i).ao_name
                    _group(id).ao_id = textures(i).ao_id
                    _group(id).colorIDmap = textures(i).colorIdMap
                    _group(id).detail_Id = textures(i).detail_id
                    _group(id).g_detailMap_id = textures(i).g_detailMap_id

                    _group(id).alphaRef = textures(i).alphaRef
                    _group(id).alphaTest = textures(i).alphaTestEnabled
                    _group(id).doubleSided = textures(i).doubleSided
                    _group(id).skinned = textures(i).skinned

                    _group(id).texture_id = i
                    Return
                End If
            Next


            Dim n_id, c_id, m_id, ao_id, detail_id, g_det_id As Integer
            updateEvent.Reset()
            Thread.Sleep(100)
            Gl.glFinish()

            c_id = get_texture_id(diffuse, id)
            n_id = get_texture_id(normal, id)
            m_id = get_texture_id(metal, id)
            ao_id = get_texture_id(ao_name, id)
            detail_id = get_texture_id(detail_name, id)
            g_det_id = get_texture_id(g_det_name, id)

            _group(id).color_name = diffuse
            _group(id).normal_name = normal
            _group(id).GMM_name = metal
            _group(id).ao_name = ao_name


            i = textures.Length - 1
            ReDim Preserve textures(i + 1)
            textures(i) = New textures_
            textures(i).c_name = diffuse
            textures(i).c_id = c_id

            textures(i).n_name = normal
            textures(i).n_id = n_id

            textures(i).gmm_name = metal
            textures(i).gmm_id = m_id

            textures(i).ao_name = ao_name
            textures(i).ao_id = ao_id

            textures(i).colorIdMap = colorIdMap

            textures(i).detail_name = detail_name
            textures(i).detail_id = detail_id

            textures(i).g_det_name = g_det_name
            textures(i).g_det_id = g_det_id

            textures(i).doubleSided = _group(id).doubleSided
            textures(i).alphaRef = _group(id).alphaRef
            textures(i).alphaTestEnabled = _group(id).alphaTest
            textures(i).skinned = _group(id).skinned

            _group(id).texture_id = i

            _group(id).color_Id = c_id
            _group(id).normal_Id = n_id
            _group(id).GMM_Id = m_id
            _group(id).ao_id = ao_id
            _group(id).detail_Id = detail_id
            _group(id).g_detailMap_id = g_det_id

            If _group(id).normal_Id > 0 Then
                _group(id).use_normapMap = 1
            End If
        Catch ex As Exception
            updateEvent.Set()
            Return
        End Try
        updateEvent.Set()

    End Sub

    Public Function get_packed_atlas(ByVal p As String, ByVal idx As Integer, atlas_mode As Integer) As String

        p = My.Settings.res_mods_path + "\" + p
        If Not p.Contains("atlas_processed") Then p = p.Replace(".atlas", ".atlas_processed")

        If Not File.Exists(p) Then
            If Not find_and_extract_file_in_pkgs(p) Then
                log_text.Append("Tried to extract but did not find: " + Path.GetFileName(p) + " at get_packed_atlas.")
                Return "File Not Found"
            End If
        End If
        stop_updating = True

        If p.Contains(".dds") Then 'pre-built atlas map?
            Dim p2 = p.Replace(".dds", "_hd.dds")
            If File.Exists(p2) Then
                p = p2
            End If
            Select Case atlas_mode
                Case ATLAS_TYPE.ATLAS_AM
                    _group(idx).AM_atlas = get_DDS_search_option(p)
                    'atlas_textures_ids.g_atlas_size.x = pass_w / 1024
                    'atlas_textures_ids.g_atlas_size.y = pass_h / 1024
                    'atlas_textures_ids.t()
                Case ATLAS_TYPE.ATLAS_GBMT
                    _group(idx).GBMT_atlas = get_DDS_search_option(p)
                Case ATLAS_TYPE.ATLAS_MAO
                    _group(idx).MAO_atlas = get_DDS_search_option(p)
            End Select
            stop_updating = False
            For i = 0 To AM_index_texture_list.Length - 1

                Select Case atlas_mode
                    Case ATLAS_TYPE.ATLAS_AM
                        AM_index_texture_list(idx).list(i) = New image_info_
                        AM_index_texture_list(idx).list(i).texture_id = _group(idx).AM_atlas
                        AM_index_texture_list(idx).list(i).name = p

                    Case ATLAS_TYPE.ATLAS_GBMT
                        GBMT_index_texture_list(idx).list(i) = New image_info_
                        GBMT_index_texture_list(idx).list(i).texture_id = _group(idx).GBMT_atlas
                        GBMT_index_texture_list(idx).list(i).name = p
                    Case ATLAS_TYPE.ATLAS_MAO
                        MAO_index_texture_list(idx).list(i) = New image_info_
                        'MAO_index_texture_list(idx).list(cnt).texture_id = grab_WorkerFbo_texture(atlas_images_coords(cnt).image_id, False)
                        MAO_index_texture_list(idx).list(i).name = p
                End Select
            Next
            _group(idx).image_size.x = 512.0!
            _group(idx).image_size.y = 512.0!
            Return "done"
        End If




        Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE)
        Dim buff() = File.ReadAllBytes(p)
        Dim ms As New MemoryStream(buff)
        Dim br As New BinaryReader(ms)
        Dim version = br.ReadInt32
        Dim atlas_width = br.ReadInt32
        Dim atlas_heigth = br.ReadInt32
        Dim useless = br.ReadInt32 ' always 1
        useless = br.ReadInt32 'magic
        useless = br.ReadInt32 'always 1
        Dim dds_chunk_size = br.ReadUInt32
        useless = br.ReadUInt32 'always 0
        'read and get the DDS file portion
        Dim dds_data(dds_chunk_size - 1) As Byte
        'not sure if this is padded..
        dds_data = br.ReadBytes(dds_chunk_size)
        '==================================================================================
        'we are not going to use this low rez map
        'Dim dds_ms = New MemoryStream(dds_data)
        'Dim Height_atlas = get_texture_from_stream(dds_ms)
        'dds_ms.Dispose()
        '==================================================================================
        ReDim atlas_images_coords(160) 'make room for textures
        Dim cnt As Integer = 0
        Gl.glEnable(Gl.GL_TEXTURE_2D)

        '==================================================================================
        'Read locations and sizes of each sub texture and its name
        While br.BaseStream.Position < br.BaseStream.Length - 1
            atlas_images_coords(cnt).loc_xs = br.ReadInt32
            atlas_images_coords(cnt).loc_xe = br.ReadInt32
            atlas_images_coords(cnt).loc_ys = br.ReadInt32
            atlas_images_coords(cnt).loc_ye = br.ReadInt32
            atlas_images_coords(cnt).width = atlas_images_coords(cnt).loc_xe - atlas_images_coords(cnt).loc_xs
            atlas_images_coords(cnt).heigth = atlas_images_coords(cnt).loc_ye - atlas_images_coords(cnt).loc_ys

            Dim ta(100) As Byte
            Dim term As Byte = 1
            Dim pnt As Integer = 0
            term = br.ReadByte
            While term <> 0
                ta(pnt) = term
                pnt += 1
                term = br.ReadByte
            End While
            ReDim Preserve ta(pnt - 1)
            atlas_images_coords(cnt).image_name = Encoding.UTF8.GetString(ta)
            atlas_images_coords(cnt).image_name = atlas_images_coords(cnt).image_name.Replace(".png", "_hd.dds")
            If atlas_images_coords(cnt).image_id > 0 Then 'clean up memory
                Gl.glDeleteTextures(1, atlas_images_coords(cnt).image_id)
                Gl.glFinish()
            End If

            'get atlas texture. Search and load it if its not in res_mods.
            atlas_images_coords(cnt).image_id = get_DDS_search_option(My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name)
            Select Case cnt
                Case _group(idx).g_atlas_indexs.x
                    log_text.AppendLine("index:" + cnt.ToString("00") + " Atlas Image: " + My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name)
                Case _group(idx).g_atlas_indexs.y
                    log_text.AppendLine("index:" + cnt.ToString("00") + " Atlas Image: " + My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name)
                Case _group(idx).g_atlas_indexs.z
                    log_text.AppendLine("index:" + cnt.ToString("00") + " Atlas Image: " + My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name)
                Case _group(idx).g_atlas_indexs.w
                    log_text.AppendLine("index:" + cnt.ToString("00") + " Atlas Image: " + My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name)

            End Select

            'set worker FBO to grab each atlas texture individually
            worker_fbo.reset_worker_fbo(atlas_images_coords(cnt).width, atlas_images_coords(cnt).heigth)

            'grab cropped texture
            Select Case atlas_mode
                Case ATLAS_TYPE.ATLAS_AM
                    AM_index_texture_list(idx).list(cnt) = New image_info_
                    AM_index_texture_list(idx).list(cnt).texture_id = grab_WorkerFbo_texture(atlas_images_coords(cnt).image_id, False)
                    AM_index_texture_list(idx).list(cnt).name = My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name
                Case ATLAS_TYPE.ATLAS_GBMT
                    GBMT_index_texture_list(idx).list(cnt) = New image_info_
                    GBMT_index_texture_list(idx).list(cnt).texture_id = grab_WorkerFbo_texture(atlas_images_coords(cnt).image_id, False)
                    GBMT_index_texture_list(idx).list(cnt).name = My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name
                Case ATLAS_TYPE.ATLAS_MAO
                    MAO_index_texture_list(idx).list(cnt) = New image_info_
                    'MAO_index_texture_list(idx).list(cnt).texture_id = grab_WorkerFbo_texture(atlas_images_coords(cnt).image_id, False)
                    MAO_index_texture_list(idx).list(cnt).name = My.Settings.res_mods_path + "\" + atlas_images_coords(cnt).image_name
            End Select
            cnt += 1
        End While
        '==================================================================================
        'resize the arrays
        ReDim Preserve atlas_images_coords(cnt - 1)
        Select Case atlas_mode
            Case ATLAS_TYPE.ATLAS_AM
                ' need these for the shader
                _group(idx).image_size.x = atlas_images_coords(0).width
                _group(idx).image_size.y = atlas_images_coords(0).heigth
                ReDim Preserve AM_index_texture_list(idx).list(cnt - 1)
            Case ATLAS_TYPE.ATLAS_GBMT
                ReDim Preserve GBMT_index_texture_list(idx).list(cnt - 1)
            Case ATLAS_TYPE.ATLAS_MAO
                ReDim Preserve MAO_index_texture_list(idx).list(cnt - 1)
        End Select
        '==================================================================================
        'build the atlas
        'create the base image....
        Dim img As Integer
        Gl.glGenTextures(1, img)
        Gl.glEnable(Gl.GL_TEXTURE_2D)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, img)
        If largestAnsio > 0 Then
            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, largestAnsio)
        End If
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE)

        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT)

        Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, atlas_width, atlas_heigth, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Nothing)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        '==================================================================================

        'setup FBO if needed
        worker_fbo.reset_worker_fbo(atlas_images_coords(0).width, atlas_images_coords(0).heigth)


        'build the atlas texture.
        For i = 0 To cnt - 1
            If worker_fbo.mWIDTH <> atlas_images_coords(i).width Or
                 worker_fbo.mHEIGTH <> atlas_images_coords(i).heigth Then
                worker_fbo.reset_worker_fbo(atlas_images_coords(i).width, atlas_images_coords(i).heigth)
            End If
            'Draw image to the FBO
            worker_fbo.draw_to_fbo_no_clip(atlas_images_coords(i).image_id)
            'copy that image to the atlas
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, img)
            Gl.glCopyTexSubImage2D(Gl.GL_TEXTURE_2D, 0,
                                   atlas_images_coords(i).loc_xs,
                                   atlas_images_coords(i).loc_ys,
                                   0,
                                   0,
                                   atlas_images_coords(i).width,
                                   atlas_images_coords(i).heigth)

            Gl.glDeleteTextures(1, atlas_images_coords(i).image_id)
            Gl.glFinish()
        Next
        '==================================================================================
        'Make it a MIPMAP texture
        img = convert_image_to_mips(img, atlas_width, atlas_heigth)
        'assign atlas texture based on type
        Dim ext As String = ""
        Select Case atlas_mode
            Case ATLAS_TYPE.ATLAS_AM
                _group(idx).AM_atlas = img
                ext = "AM_" + idx.ToString("00")
            Case ATLAS_TYPE.ATLAS_GBMT
                _group(idx).GBMT_atlas = img
                ext = "GBMT_" + idx.ToString("00")
            Case ATLAS_TYPE.ATLAS_MAO
                _group(idx).MAO_atlas = img
                ext = "MAO_" + idx.ToString("00")
        End Select
        'debug drawing of atlas to wot_temp
        'save_atlas_map("atlas_" + ext, atlas_width, atlas_heigth, img)
        '==================================================================================
        'below is debug shit.
        'Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0) 'rebind default FBO

        'w = frmMain.pb1.Width
        'h = frmMain.pb1.Height
        'ResizeGL(w, h)

        'Gl.glMatrixMode(Gl.GL_PROJECTION) 'Select Projection
        'Gl.glLoadIdentity() 'Reset The Matrix
        'Gl.glOrtho(0, w, -h, 0, -200.0, 100.0) 'Select Ortho Mode
        'Gl.glMatrixMode(Gl.GL_MODELVIEW)    'Select Modelview Matrix
        'Gl.glLoadIdentity() 'Reset The Matrix
        'Gl.glDisable(Gl.GL_DEPTH_TEST)

        'Gl.glBindTexture(Gl.GL_TEXTURE_2D, img)

        'frmMain.draw_main_rec(New Point, w, h)
        'Gdi.SwapBuffers(pb1_hDC)
        'Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)

        Gl.glDisable(Gl.GL_TEXTURE_2D)
        stop_updating = False
        Return "OK"
    End Function
    Private Sub save_atlas_map(ByVal file_name_out As String, ByVal w As Integer, ByVal h As Integer, ByVal img As Integer)
        worker_fbo.reset_worker_fbo(w, h)
        worker_fbo.draw_to_fbo_no_clip(img)
        Gl.glFinish()
        Dim tId As Integer = Il.ilGenImage
        Il.ilBindImage(tId)
        Il.ilTexImage(w, h, 0, 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE, Nothing)
        Il.ilEnable(Il.IL_FILE_OVERWRITE)
        Gl.glReadPixels(0, 0, w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Il.ilGetData())
        Gl.glFinish()
        file_name_out = Temp_Storage + "\" + file_name_out + ".png"
        Il.ilSave(Il.IL_PNG, file_name_out) ' save to temp
        Dim result = Il.ilGetError
        Gl.glDisable(Gl.GL_TEXTURE_2D)
        Il.ilBindImage(0)
        Il.ilDeleteImage(tId)

    End Sub
    Private Function convert_image_to_mips(ByVal img As Integer, ByVal width As Integer, ByVal heigth As Integer) As Integer
        worker_fbo.reset_worker_fbo(width, heigth)
        worker_fbo.draw_to_fbo_no_clip(img)
        Gl.glFinish()
        Dim Id As Integer = Il.ilGenImage
        Il.ilBindImage(Id)
        Il.ilTexImage(width, heigth, 0, 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE, Nothing)

        Gl.glBindTexture(Gl.GL_TEXTURE_2D, img)
        If largestAnsio > 0 Then
            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, largestAnsio)
        End If
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE)

        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP)

        Gl.glGetTexImage(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, Gl.GL_UNSIGNED_BYTE, Il.ilGetData())


        Dim e1 = Gl.glGetError

        'Gl.glDeleteTextures(1, img)
        Gl.glFinish()

        Il.ilBindImage(0)
        Il.ilDeleteImage(Id)
        Gl.glFinish()
        Return img

    End Function


    Private Function grab_WorkerFbo_texture(ByVal t_id As Integer, ByVal no_clip As Boolean) As Integer
        'no_clip = true it does clip off the wrap border for atlas use.

        Dim img As Integer
        Gl.glGenTextures(1, img)
        Gl.glEnable(Gl.GL_TEXTURE_2D)
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, img)

        If largestAnsio > 0 Then
            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, largestAnsio)
        End If
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE)

        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE)
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE)
        Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, WorkFBO.worker_fbo.mWIDTH, WorkFBO.worker_fbo.mHEIGTH, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, Nothing)
        Dim e1 = Gl.glGetError
        If no_clip Then
            worker_fbo.draw_to_fbo_no_clip(t_id)
        Else
            worker_fbo.draw_to_fbo(t_id)
        End If
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, img)
        Dim e2 = Gl.glGetError
        Gl.glCopyTexSubImage2D(Gl.GL_TEXTURE_2D, 0,
                       0,
                       0,
                       0,
                       0,
                       WorkFBO.worker_fbo.mHEIGTH,
                       WorkFBO.worker_fbo.mHEIGTH)
        Dim e3 = Gl.glGetError
        Gl.glFinish()
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Return img
    End Function
    Public Function get_DDS_search_option(ByRef p As String) As Integer
        'loads DDS.. Asked to find it if it does not exist in res_mods
        p = p.Replace("/", "\")
        p = p.Replace(My.Settings.res_mods_path + "\", "")
        If Not File.Exists(My.Settings.res_mods_path + "\" + p) Then
            'If MsgBox(Path.GetFileName(p) + vbCrLf + " was not found in res_mods" + vbCrLf + _
            '        "Would you like me to extract it from the PKG files?", MsgBoxStyle.YesNo, "File Not Found...") = MsgBoxResult.Yes Then
            If Not find_and_extract_file_in_pkgs(p) Then
                Return 0
            Else
                Return load_dds_file_Clamp(My.Settings.res_mods_path + "\" + p)
            End If
        Else
            Return load_dds_file_Clamp(My.Settings.res_mods_path + "\" + p)
        End If
        'Else
        'End If
        Return 0
    End Function
    Public Function find_tank_and_extract_file_in_pkgs(ByVal p As String) As Boolean
        'Searched and extracts file p to res_mods.
        'This does NOT overwrite existing files.
        Dim pp = p.Replace(My.Settings.res_mods_path + "\", "") ' strip res_mods path off head of file name
        Dim ss = pp.ToLower.Replace("\", "/")
        'Check if this texture is to be extracted by tank component setting.
        If Not frmExtract.ext_chassis.Checked And ss.Contains("track") Then
            Return True
        End If
        If Not frmExtract.ext_chassis.Checked And ss.Contains("chassis") Then
            Return True
        End If
        If Not frmExtract.ext_hull.Checked And ss.Contains("hull") Then
            Return True
        End If
        If Not frmExtract.ext_turret.Checked And ss.Contains("turret") Then
            Return True
        End If
        If Not frmExtract.ext_gun.Checked And ss.Contains("gun") Then
            Return True
        End If
        Dim pkName = Find_entry(ss)
        If Not String.IsNullOrEmpty(pkName) Then
            Using zipf As ZipFile = New ZipFile(Path.GetDirectoryName(shared_pkg_search_list(0)) + "\" + pkName)
                Dim entry As ZipEntry = zipf(ss)
                If entry IsNot Nothing Then
                    entry.Extract(My.Settings.res_mods_path + "\", ExtractExistingFileAction.DoNotOverwrite)
                    Return True
                End If
            End Using
        End If

        For Each f In tank_pkg_search_list
            Using zipf As New ZipFile(f)
                For Each entry In zipf
                    If Not entry.IsDirectory Then
                        If entry.FileName.ToLower = ss Then
                            entry.Extract(My.Settings.res_mods_path + "\", ExtractExistingFileAction.DoNotOverwrite)
                            zipf.Dispose()
                            GC.Collect()
                            Return True
                        End If
                    End If
                Next
            End Using
        Next
        Return False
    End Function
    Public Function find_tank_and_return_entry_in_pkgs(ByVal p As String) As ZipEntry
        'Searched and extracts file p to res_mods.
        'This does NOT overwrite existing files.
        Dim pp = p.Replace(My.Settings.res_mods_path + "\", "") ' strip res_mods path off head of file name
        Dim ss = pp.ToLower.Replace("\", "/")
        'Check if this texture is to be extracted by tank component setting.
        If Not frmExtract.ext_chassis.Checked And ss.Contains("track") Then
            Return Nothing
        End If
        If Not frmExtract.ext_chassis.Checked And ss.Contains("chassis") Then
            Return Nothing
        End If
        If Not frmExtract.ext_hull.Checked And ss.Contains("hull") Then
            Return Nothing
        End If
        If Not frmExtract.ext_turret.Checked And ss.Contains("turret") Then
            Return Nothing
        End If
        If Not frmExtract.ext_gun.Checked And ss.Contains("gun") Then
            Return Nothing
        End If
        Dim pkName = Find_entry(ss)
        If Not String.IsNullOrEmpty(pkName) Then
            Using zipf As ZipFile = New ZipFile(Path.GetDirectoryName(shared_pkg_search_list(0)) + "\" + pkName)
                Dim entry As ZipEntry = zipf(ss)
                If entry IsNot Nothing Then
                    Return entry
                End If
            End Using
        End If
        For Each f In tank_pkg_search_list
            Using zipf As New ZipFile(f)
                For Each entry In zipf
                    If Not entry.IsDirectory Then
                        If entry.FileName.Contains("vehicles") Then

                            'Debug.WriteLine(entry.FileName)
                            If entry.FileName.ToLower = ss Then
                                zipf.Dispose()
                                GC.Collect()
                                Return entry
                            End If
                        End If
                    End If
                Next
            End Using
        Next
        Return Nothing
    End Function
    Public Function find_tank_part_return_entry(ByVal p As String) As ZipEntry
        'Searched and extracts file p to res_mods.
        'This does NOT overwrite existing files.
        Dim pp = p.Replace(My.Settings.res_mods_path + "\", "") ' strip res_mods path off head of file name
        Dim ss = pp.ToLower.Replace("\", "/")

        Dim pkName = Find_entry(ss)
        If Not String.IsNullOrEmpty(pkName) Then
            Using zipf As ZipFile = New ZipFile(Path.GetDirectoryName(shared_pkg_search_list(0)) + "\" + pkName)
                Dim entry As ZipEntry = zipf(ss)
                If entry IsNot Nothing Then
                    Return entry
                End If
            End Using
        End If

        'this is slow
        For Each f In tank_pkg_search_list
            Using zipf As New ZipFile(f)
                For Each entry In zipf
                    If Not entry.IsDirectory Then
                        If entry.FileName.Contains("vehicles") Then

                            'Debug.WriteLine(entry.FileName)
                            If entry.FileName.ToLower = ss Then
                                zipf.Dispose()
                                GC.Collect()
                                Return entry
                            End If
                        End If
                    End If
                Next
            End Using
        Next
        Return Nothing
    End Function
    Public Function find_and_extract_file_in_pkgs(ByVal p As String) As Boolean
        'Searched and extracts file p to res_mods.
        'This does NOT overwrite existing files.
        Dim pp = p.Replace(My.Settings.res_mods_path + "\", "") ' strip res_mods path off head of file name
        Dim ss = pp.ToLower.Replace("\", "/")

        Dim pkName = Find_entry(ss)
        If Not String.IsNullOrEmpty(pkName) Then
            Using zipf As ZipFile = New ZipFile(Path.GetDirectoryName(shared_pkg_search_list(0)) + "\" + pkName)
                Dim entry As ZipEntry = zipf(ss)
                If entry IsNot Nothing Then
                    entry.Extract(My.Settings.res_mods_path + "\", ExtractExistingFileAction.DoNotOverwrite)
                    Return True
                End If
            End Using
        End If

        If Not String.IsNullOrEmpty(pkName) Then
            Using zipf As ZipFile = New ZipFile(Path.GetDirectoryName(shared_pkg_search_list(0)) + "\" + pkName)
                Dim entry As ZipEntry = zipf(ss)
                If entry IsNot Nothing Then
                    entry.Extract(My.Settings.res_mods_path + "\", ExtractExistingFileAction.DoNotOverwrite)
                    Return True
                End If
            End Using
        End If
        For Each f In pkg_search_list
            Using zipf As New ZipFile(f)
                For Each entry In zipf
                    If Not entry.IsDirectory Then
                        If entry.FileName.ToLower = ss Then
                            entry.Extract(My.Settings.res_mods_path + "\", ExtractExistingFileAction.DoNotOverwrite)
                            zipf.Dispose()
                            GC.Collect()
                            Return True
                        End If
                    End If
                Next
            End Using
        Next
        Return False
    End Function

    Public Function get_texture_id(name As String, g_id As Integer) As Integer
        Dim id As Integer
        If name Is Nothing Then name = ""
        Dim ent As Ionic.Zip.ZipEntry = Nothing
        If name = "" Then Return -1
        If My.Settings.res_mods_path.Contains("res_mods") Then
            'GoTo skip_hd
            Dim r_path = My.Settings.res_mods_path + "\" + name
            Dim r_pathSD = My.Settings.res_mods_path + "\" + name
            If name.Contains("res_mods") Then
                r_path = name
            End If
            If File.Exists(r_path) Then
                log_text.AppendLine("loaded HD res_mods :  " + Path.GetFileName(name))
                Dim raw = File.ReadAllBytes(r_path)
                mStream = New MemoryStream(raw)
                id = get_texture_fast(mStream)
                Return id
            End If
            If File.Exists(r_pathSD) Then
                log_text.AppendLine("loaded SD res_mods : " + Path.GetFileName(name))
                Dim raw = File.ReadAllBytes(r_pathSD)
                mStream = New MemoryStream(raw)
                id = get_texture_fast(mStream)
                Return id
            End If
        End If
        'No texture found in res_mods so..... try PKG files
        Try 'look in HD packages

            Dim pkName = Find_entry(name)
            If Not String.IsNullOrEmpty(pkName) Then
                Using zipf As ZipFile = New ZipFile(My.Settings.game_path + "\res\packages\" + pkName)
                    ent = zipf(name)
                    If ent IsNot Nothing Then
                        mStream = New MemoryStream
                        ent.Extract(mStream)
                        id = get_texture_fast(mStream) ' get hd texture ID
                        Return id
                    End If

                End Using
            End If

        Catch ex As Exception
        End Try

        If ent Is Nothing Then ' look in shared content
            ent = search_shared_pkgs(name) ' look in tank package
        End If
        If PRIMITIVES_MODE Then
            id = get_DDS_search_option(name)
            If id > 0 Then
                log_text.AppendLine("loaded HD from PKG : " + Path.GetFileName(name))
                Return id
            End If
            id = get_DDS_search_option(name)
            If id > 0 Then
                log_text.AppendLine("loaded SD from PKG : " + Path.GetFileName(name))
                Return id
            End If
        End If
        If ent Is Nothing Then
            ent = find_tank_and_return_entry_in_pkgs(name)
        End If
        If ent IsNot Nothing Then
            'it was found as HD abouve
            log_text.AppendLine("loaded HD from PKG : " + Path.GetFileName(name))
            mStream = New MemoryStream
            ent.Extract(mStream)
            id = get_texture_fast(mStream) ' get hd texture ID
            Return id
        Else
            'look in current pkg for SD texture
skip_hd:
            If ent Is Nothing Then 'if not found in current pkg than look in shared
                If frmMain.packages_2(current_tank_package) IsNot Nothing Then
                    ent = frmMain.packages_2(current_tank_package)(name) ' look in 2nd tank package
                End If
            End If
            If ent Is Nothing Then 'if not found in current pkg than look in shared
                ent = search_shared_pkgs(name) ' look in 2nd tank package
            End If

            If ent Is Nothing Then
                ent = find_tank_and_return_entry_in_pkgs(name)
            End If

            If ent IsNot Nothing Then ' found SD above
                log_text.AppendLine("loaded SD from PKG : " + Path.GetFileName(name))
                mStream = New MemoryStream
                ent.Extract(mStream)
                id = get_texture_fast(mStream) ' get SD texture ID
                Return id
            End If
        End If

        'never found the texture period! Log it!
        log_text.AppendLine("Cant find:" + name)

        Return 0
    End Function
    Public Function get_texture_fast(ByRef ms As MemoryStream) As Integer
        ' Get the byte array from the memory stream
        Dim buffer() As Byte = ms.ToArray()

        ' Pin the array in memory
        Dim gch As GCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned)
        Dim ptr As IntPtr = gch.AddrOfPinnedObject()

        ' Call the function with pointer and length
        Dim textureID As Integer = LoadTextureFromMemory(ptr, buffer.Length)

        ' Free the pinned handle
        gch.Free()

        Return textureID

    End Function

    Public Function get_texture_and_bitmap(ByRef ms As MemoryStream, file_path As String, ByRef bmp As Bitmap) As Integer

        ' Load the texture using LoadTextureFromMemory and get the texture ID
        ' Get the byte array from the memory stream
        Dim buffer() As Byte = ms.ToArray()

        ' Pin the array in memory
        Dim gch As GCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned)
        Dim ptr As IntPtr = gch.AddrOfPinnedObject()

        ' Call the function with pointer and length
        Dim textureID As Integer = LoadTextureFromMemory(ptr, buffer.Length)

        ' Check if the texture was loaded successfully
        If textureID = 0 Then
            MsgBox("Failed to load texture from memory.", MsgBoxStyle.Critical, "Error")
            Return 0
        End If

        ' Bind the texture and retrieve its dimensions
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID)

        Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
        Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)

        ' Create the bitmap and lock its bits for writing
        Dim rect As Rectangle = New Rectangle(0, 0, width, height)
        bmp = New System.Drawing.Bitmap(width, height, PixelFormat.Format24bppRgb)

        Dim bitmapData As BitmapData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb)

        ' Use DevIL to convert the texture data into bitmap pixel data
        Dim success As Integer = Il.ilConvertImage(Il.IL_BGR, Il.IL_UNSIGNED_BYTE)

        If success = Il.IL_NO_ERROR Then
            Il.ilCopyPixels(0, 0, 0, width, height, 1, Il.IL_BGR, Il.IL_UNSIGNED_BYTE, bitmapData.Scan0)
            bmp.UnlockBits(bitmapData)
        Else
            MsgBox("Failed to convert texture to bitmap.", MsgBoxStyle.Critical, "Error")
            Return 0
        End If

        ' Unbind texture and clean up
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
        Return textureID
    End Function

    Public Function get_png(ByVal ms As MemoryStream) As Bitmap
        'Dim s As String = ""
        's = Gl.glGetError
        Dim image_id As Integer = -1
        'Dim app_local As String = Application.StartupPath.ToString

        Dim texID As UInt32
        Dim textIn(ms.Length) As Byte
        ms.Position = 0
        ms.Read(textIn, 0, ms.Length)
        texID = Ilu.iluGenImage() ' /* Generation of one image name */
        Il.ilBindImage(texID) '; /* Binding of image name */
        Dim success = Il.ilGetError
        Il.ilLoadL(Il.IL_PNG, textIn, textIn.Length)
        success = Il.ilGetError
        If success = Il.IL_NO_ERROR Then
            'Ilu.iluFlipImage()
            'Ilu.iluMirror()
            Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
            Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)

            ' Create the bitmap.
            Dim Bitmapi = New System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb)
            Dim rect As Rectangle = New Rectangle(0, 0, width, height)

            ' Store the DevIL image data into the bitmap.
            Dim bitmapData As BitmapData = Bitmapi.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)

            Il.ilConvertImage(Il.IL_BGRA, Il.IL_UNSIGNED_BYTE)
            Il.ilCopyPixels(0, 0, 0, width, height, 1, Il.IL_BGRA, Il.IL_UNSIGNED_BYTE, bitmapData.Scan0)
            Bitmapi.UnlockBits(bitmapData)

            'If your image contains alpha channel you can replace IL_RGB with IL_RGBA */
            'If make_id Then

            '    Gl.glGenTextures(1, image_id)
            '    Gl.glEnable(Gl.GL_TEXTURE_2D)
            '    Gl.glBindTexture(Gl.GL_TEXTURE_2D, image_id)
            '    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
            '    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR)
            '    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE)

            '    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT)
            '    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT)

            '    Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Il.ilGetInteger(Il.IL_IMAGE_BPP), Il.ilGetInteger(Il.IL_IMAGE_WIDTH), _
            '                    Il.ilGetInteger(Il.IL_IMAGE_HEIGHT), 0, Il.ilGetInteger(Il.IL_IMAGE_FORMAT), Gl.GL_UNSIGNED_BYTE, _
            '                    Il.ilGetData()) '  Texture specification 
            '    Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            '    Il.ilBindImage(0)
            '    'ilu.iludeleteimage(texID)
            '    ReDim Preserve map_texture_ids(index + 1)
            '    map_texture_ids(index) = image_id
            'End If

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            Il.ilBindImage(0)
            Ilu.iluDeleteImage(texID)
            'GC.Collect()
            Return Bitmapi
        Else
            MsgBox("can't find thumb image of tank", MsgBoxStyle.Critical, "oops")
        End If
        Return Nothing
    End Function

    Public Function get_png_id(ByVal ms As MemoryStream) As Integer
        'Dim s As String = ""
        's = Gl.glGetError

        updateEvent.Reset()
        Thread.Sleep(200)
        Dim image_id As Integer = -1
        'Dim app_local As String = Application.StartupPath.ToString

        Dim texID As UInt32
        Dim textIn(ms.Length) As Byte
        ms.Position = 0
        ms.Read(textIn, 0, ms.Length)
        texID = Ilu.iluGenImage() ' /* Generation of one image name */
        Il.ilBindImage(texID) '; /* Binding of image name */
        Dim success = Il.ilGetError
        Il.ilLoadL(Il.IL_PNG, textIn, textIn.Length)
        success = Il.ilGetError
        If success = Il.IL_NO_ERROR Then
            Dim e = Gl.glGetError
            'Ilu.iluFlipImage()
            'Ilu.iluMirror()
            Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
            Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)


            'If your image contains alpha channel you can replace IL_RGB with IL_RGBA */
            'If make_id Then

            Gl.glGenTextures(1, image_id)
            e = Gl.glGetError
            Gl.glEnable(Gl.GL_TEXTURE_2D)
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, image_id)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR)

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT)
            e = Gl.glGetError

            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Il.ilGetInteger(Il.IL_IMAGE_BPP), Il.ilGetInteger(Il.IL_IMAGE_WIDTH),
                            Il.ilGetInteger(Il.IL_IMAGE_HEIGHT), 0, Il.ilGetInteger(Il.IL_IMAGE_FORMAT), Gl.GL_UNSIGNED_BYTE,
                            Il.ilGetData()) '  Texture specification 
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            Il.ilBindImage(0)
            'ilu.iludeleteimage(texID)
            e = Gl.glGetError

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            Il.ilBindImage(0)
            Ilu.iluDeleteImage(texID)
            'GC.Collect()
            updateEvent.Set()
            Return image_id
        Else
            MsgBox("can't load MS PNG", MsgBoxStyle.Critical, "oops")
        End If
        updateEvent.Set()
        Return Nothing
    End Function

    Public Function load_png_file(ByVal fs As String) As Integer
        Dim image_id As Integer = -1
        Dim texID As UInt32

        updateEvent.Reset()

        texID = Ilu.iluGenImage() ' /* Generation of one image name */
        Il.ilBindImage(texID) '; /* Binding of image name */
        Dim success = Il.ilGetError
        Il.ilLoad(Il.IL_PNG, fs)
        success = Il.ilGetError
        If success = Il.IL_NO_ERROR Then
            'Ilu.iluFlipImage()
            Ilu.iluMirror()
            Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
            Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)


            Il.ilConvertImage(Il.IL_BGRA, Il.IL_UNSIGNED_BYTE)

            success = Il.ilConvertImage(Il.IL_RGBA, Il.IL_UNSIGNED_BYTE) ' Convert every colour component into unsigned bytes
            'If your image contains alpha channel you can replace IL_RGB with IL_RGBA */
            Gl.glGenTextures(1, image_id)
            Gl.glEnable(Gl.GL_TEXTURE_2D)
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, image_id)
            If largestAnsio > 0 Then
                Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, largestAnsio)
            End If
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE)

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT)

            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Il.ilGetInteger(Il.IL_IMAGE_BPP), Il.ilGetInteger(Il.IL_IMAGE_WIDTH),
            Il.ilGetInteger(Il.IL_IMAGE_HEIGHT), 0, Il.ilGetInteger(Il.IL_IMAGE_FORMAT), Gl.GL_UNSIGNED_BYTE,
            Il.ilGetData()) '  Texture specification 
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            Il.ilBindImage(0)
            Ilu.iluDeleteImage(texID)
            updateEvent.Set()
            Return image_id
        Else
            log_text.AppendLine("Png did not load:" + fs)
        End If
        updateEvent.Set()
        Return Nothing
    End Function
    Dim pass_w, pass_h As Integer
    Public Function load_dds_file_Clamp(ByVal fs As String) As Integer
        Dim image_id As Integer = -1

        Dim texID As UInt32
        texID = Ilu.iluGenImage() ' /* Generation of one image name */
        Il.ilBindImage(texID) '; /* Binding of image name */
        Dim success = Il.ilGetError
        Il.ilLoad(Il.IL_DDS, fs)
        success = Il.ilGetError
        If success = Il.IL_NO_ERROR Then
            'Ilu.iluFlipImage()
            ' Ilu.iluMirror()
            Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
            Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)
            pass_h = height
            pass_w = width
            'Dim dds_format = Il.ilGetInteger(Il.IL_DXTC_DATA_FORMAT)
            'Debug.WriteLine(dds_format.ToString)

            'Il.ilConvertImage(Il.IL_BGRA, Il.IL_UNSIGNED_BYTE)

            'success = Il.ilConvertImage(Il.IL_RGBA, Il.IL_UNSIGNED_BYTE)

            Gl.glGenTextures(1, image_id)
            Gl.glEnable(Gl.GL_TEXTURE_2D)
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, image_id)
            If largestAnsio > 0 Then
                Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, largestAnsio)
            End If
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE)

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT)

            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, _
            Il.ilGetData()) '  Texture specification 


            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            Il.ilBindImage(0)
            Ilu.iluDeleteImage(texID)
            Gl.glFinish()
            Return image_id
        Else
            log_text.AppendLine("File Missing: " + fs)
        End If
        Return Nothing
    End Function
    Public Function load_dds_file(ByVal fs As String) As Integer
        Return LoadTextureDDS(fs)
    End Function
    Public Function load_jpg_file(ByVal fs As String) As Integer
        Dim image_id As Integer = -1

        Dim texID As UInt32
        texID = Ilu.iluGenImage() ' /* Generation of one image name */
        Il.ilBindImage(texID) '; /* Binding of image name */
        Dim success = Il.ilGetError
        Il.ilLoad(Il.IL_JPG, fs)
        success = Il.ilGetError
        If success = Il.IL_NO_ERROR Then
            'Ilu.iluFlipImage()
            Ilu.iluMirror()
            Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
            Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)


            Il.ilConvertImage(Il.IL_BGRA, Il.IL_UNSIGNED_BYTE)

            success = Il.ilConvertImage(Il.IL_RGBA, Il.IL_UNSIGNED_BYTE) ' Convert every colour component into unsigned bytes
            'If your image contains alpha channel you can replace IL_RGB with IL_RGBA */
            Gl.glGenTextures(1, image_id)
            Gl.glEnable(Gl.GL_TEXTURE_2D)
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, image_id)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR)

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT)
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT)

            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Il.ilGetInteger(Il.IL_IMAGE_BPP), Il.ilGetInteger(Il.IL_IMAGE_WIDTH), _
            Il.ilGetInteger(Il.IL_IMAGE_HEIGHT), 0, Il.ilGetInteger(Il.IL_IMAGE_FORMAT), Gl.GL_UNSIGNED_BYTE, _
            Il.ilGetData()) '  Texture specification 
            Gl.glGenerateMipmapEXT(Gl.GL_TEXTURE_2D)
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0)
            Il.ilBindImage(0)
            Ilu.iluDeleteImage(texID)
            Return image_id
        Else
            log_text.AppendLine("File Missing: " + fs)
        End If
        Return Nothing
    End Function

    Public Function get_texture_from_stream(ByRef ms As MemoryStream) As Integer

        ' Get the byte array from the memory stream
        Dim buffer() As Byte = ms.ToArray()

        ' Pin the array in memory
        Dim gch As GCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned)
        Dim ptr As IntPtr = gch.AddrOfPinnedObject()

        ' Call the function with pointer and length
        Dim textureID As Integer = LoadTextureFromMemory(ptr, buffer.Length)

        ' Free the pinned handle
        gch.Free()

        Return textureID


    End Function

    Public Function get_image(ByVal file As String) As Image
        'Dim s As String = ""
        's = Gl.glGetError
        Dim image_id As Integer = -1
        'Dim app_local As String = Application.StartupPath.ToString

        Dim texID As UInt32
        texID = Ilu.iluGenImage() ' /* Generation of one image name */
        Il.ilBindImage(texID) '; /* Binding of image name */
        Dim success = Il.ilGetError
        Il.ilLoadImage(file)
        success = Il.ilGetError
        If success = Il.IL_NO_ERROR Then
            'Ilu.iluFlipImage()
            'Ilu.iluMirror()
            Dim width As Integer = Il.ilGetInteger(Il.IL_IMAGE_WIDTH)
            Dim height As Integer = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT)

            ' Create the bitmap.
            Dim Bitmapi = New System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb)
            Dim rect As Rectangle = New Rectangle(0, 0, width, height)

            ' Store the DevIL image data into the bitmap.
            Dim bitmapData As BitmapData = Bitmapi.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)

            Il.ilConvertImage(Il.IL_BGRA, Il.IL_UNSIGNED_BYTE)
            Il.ilCopyPixels(0, 0, 0, width, height, 1, Il.IL_BGRA, Il.IL_UNSIGNED_BYTE, bitmapData.Scan0)
            Bitmapi.UnlockBits(bitmapData)

            Il.ilBindImage(0)
            Ilu.iluDeleteImage(texID)
            GC.Collect()
            Dim istream As New MemoryStream
            Bitmapi.Save(istream, ImageFormat.Png)
            Bitmapi.Dispose()
            Return Image.FromStream(istream)
        Else
            MsgBox("png load error!", MsgBoxStyle.Exclamation, "Oh No!!")
            Return Nothing
        End If
        Return Nothing
    End Function


End Module
