﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;
using Twinsanity;

namespace TwinsaityEditor
{
    public class RMViewer : ThreeDViewer
    {
        //add to preferences later
        private static Color[] colors = new[] { Color.Gray, Color.Green, Color.Red, Color.DarkBlue, Color.Yellow, Color.Pink, Color.DarkCyan, Color.DarkGreen, Color.DarkRed, Color.Brown, Color.DarkMagenta, Color.Orange, Color.DarkSeaGreen, Color.Bisque, Color.Coral };

        private bool show_col_nodes, show_triggers;
        private int dlist_col = -1, dlist_trg = -1;
        private int[] dlist_inst = new int[7] { -1, -1, -1, -1, -1, -1, -1 };
        private int[] dlist_trig = new int[7] { -1, -1, -1, -1, -1, -1, -1 };
        private ColData data;
        private TwinsFile file;

        private float indicator_size = 0.5f;

        public static bool[] InstancesChanged { get; set; } = new bool[7] { false, false, false, false, false, false, false };

        public RMViewer(ColData data, ref TwinsFile file)
        {
            //initialize variables here
            dlist_col = dlist_trg = -1;
            show_col_nodes = show_triggers = false;
            this.data = data;
            this.file = file;
        }

        protected override void RenderObjects()
        {
            //put all object rendering code here
            //draw collision
            if (data != null)
            {
                if (dlist_col == -1) //if collision tree display list is non-existant
                {
                    dlist_col = GL.GenLists(1);
                    GL.NewList(dlist_col, ListMode.CompileAndExecute);
                    GL.Begin(PrimitiveType.Triangles);
                    foreach (var tri in data.Tris)
                    {
                        GL.Color3(colors[tri.Surface % colors.Length]);
                        Vector3 v1 = new Vector3(-data.Vertices[tri.Vert1].X, data.Vertices[tri.Vert1].Y, data.Vertices[tri.Vert1].Z);
                        Vector3 v2 = new Vector3(-data.Vertices[tri.Vert2].X, data.Vertices[tri.Vert2].Y, data.Vertices[tri.Vert2].Z);
                        Vector3 v3 = new Vector3(-data.Vertices[tri.Vert3].X, data.Vertices[tri.Vert3].Y, data.Vertices[tri.Vert3].Z);
                        GL.Normal3(Utils.VectorFuncs.CalcNormal(v1, v2, v3));
                        GL.Vertex3(v1.X, v1.Y, v1.Z);
                        GL.Vertex3(v2.X, v2.Y, v2.Z);
                        GL.Vertex3(v3.X, v3.Y, v3.Z);
                    }
                    GL.End();
                    GL.EndList();
                }
                else
                    GL.CallList(dlist_col);

                if (show_col_nodes)
                {
                    GL.Disable(EnableCap.Lighting);
                    if (dlist_trg == -1)
                    {
                        dlist_trg = GL.GenLists(1);
                        GL.NewList(dlist_trg, ListMode.CompileAndExecute);
                        foreach (var i in data.Triggers)
                        {
                            if (i.Flag1 == i.Flag2 && i.Flag1 < 0)
                                GL.Color3(Color.Cyan);
                            else
                                GL.Color3(Color.Red);
                            GL.Begin(PrimitiveType.LineStrip);
                            GL.Vertex3(-i.X1, i.Y1, i.Z1);
                            GL.Vertex3(-i.X2, i.Y1, i.Z1);
                            GL.Vertex3(-i.X2, i.Y2, i.Z1);
                            GL.Vertex3(-i.X1, i.Y2, i.Z1);
                            GL.Vertex3(-i.X1, i.Y1, i.Z1);
                            GL.Vertex3(-i.X1, i.Y1, i.Z2);
                            GL.Vertex3(-i.X2, i.Y1, i.Z2);
                            GL.Vertex3(-i.X2, i.Y1, i.Z1);
                            GL.End();
                            GL.Begin(PrimitiveType.LineStrip);
                            GL.Vertex3(-i.X1, i.Y1, i.Z2);
                            GL.Vertex3(-i.X1, i.Y2, i.Z2);
                            GL.Vertex3(-i.X2, i.Y2, i.Z2);
                            GL.Vertex3(-i.X2, i.Y1, i.Z2);
                            GL.End();
                            GL.Begin(PrimitiveType.Lines);
                            GL.Vertex3(-i.X1, i.Y2, i.Z2);
                            GL.Vertex3(-i.X1, i.Y2, i.Z1);
                            GL.Vertex3(-i.X2, i.Y2, i.Z2);
                            GL.Vertex3(-i.X2, i.Y2, i.Z1);
                            GL.End();
                        }
                        GL.EndList();
                    }
                    else
                        GL.CallList(dlist_trg);
                    GL.Enable(EnableCap.Lighting);
                }
            }

            //Draw instances (solid surfaces)
            for (uint i = 0; i <= 7; ++i)
            {
                if (file.SecInfo.Records.ContainsKey(i))
                {
                    if (InstancesChanged[i])
                    {
                        GL.DeleteLists(dlist_inst[i], 1);
                        dlist_inst[i] = -1;
                        InstancesChanged[i] = false;
                    }
                    if (dlist_inst[i] == -1)
                    {
                        dlist_inst[i] = GL.GenLists(1);
                        GL.NewList(dlist_inst[i], ListMode.CompileAndExecute);
                        if (((TwinsSection)file.SecInfo.Records[i]).SecInfo.Records.ContainsKey(6))
                        {
                            foreach (Instance j in ((TwinsSection)((TwinsSection)file.SecInfo.Records[i]).SecInfo.Records[6]).SecInfo.Records.Values)
                            {
                                GL.PushMatrix();
                                GL.Disable(EnableCap.Lighting);
                                GL.Translate(-j.Pos.X, j.Pos.Y, j.Pos.Z);
                                GL.Rotate(-j.RotX / (float)ushort.MaxValue * 360f, 1, 0, 0);
                                GL.Rotate(-j.RotY / (float)ushort.MaxValue * 360f, 0, 1, 0);
                                GL.Rotate(-j.RotZ / (float)ushort.MaxValue * 360f, 0, 0, 1);
                                DrawAxes(0, 0, 0, 0.5f);
                                GL.Color3(colors[colors.Length - i - 1]);
                                GL.Begin(PrimitiveType.LineStrip);
                                GL.Vertex3(-indicator_size, -indicator_size + 0.5, -indicator_size);
                                GL.Vertex3(+indicator_size, -indicator_size + 0.5, -indicator_size);
                                GL.Vertex3(+indicator_size, +indicator_size + 0.5, -indicator_size);
                                GL.Vertex3(-indicator_size, +indicator_size + 0.5, -indicator_size);
                                GL.Vertex3(-indicator_size, -indicator_size + 0.5, -indicator_size);
                                GL.Vertex3(-indicator_size, -indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(+indicator_size, -indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(+indicator_size, -indicator_size + 0.5, -indicator_size);
                                GL.End();
                                GL.Begin(PrimitiveType.LineStrip);
                                GL.Vertex3(-indicator_size, -indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(-indicator_size, +indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(+indicator_size, +indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(+indicator_size, -indicator_size + 0.5, +indicator_size);
                                GL.End();
                                GL.Begin(PrimitiveType.Lines);
                                GL.Vertex3(-indicator_size, +indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(-indicator_size, +indicator_size + 0.5, -indicator_size);
                                GL.Vertex3(+indicator_size, +indicator_size + 0.5, +indicator_size);
                                GL.Vertex3(+indicator_size, +indicator_size + 0.5, -indicator_size);
                                GL.End();
                                RenderString(j.ID.ToString());
                                GL.Enable(EnableCap.Lighting);
                                GL.PopMatrix();
                            }
                        }
                        GL.EndList();
                    }
                    else
                        GL.CallList(dlist_inst[i]);
                }
            }

            //Draw triggers (transparent surfaces)
            for (uint i = 0; i <= 7; ++i)
            {
                if (file.SecInfo.Records.ContainsKey(i))
                {
                    if (((TwinsSection)file.SecInfo.Records[i]).SecInfo.Records.ContainsKey(7) && show_triggers)
                    {
                        foreach (Trigger j in ((TwinsSection)((TwinsSection)file.SecInfo.Records[i]).SecInfo.Records[7]).SecInfo.Records.Values)
                        {
                            GL.PushMatrix();
                            GL.Translate(-j.Coords[1].X, j.Coords[1].Y, j.Coords[1].Z);

                            GL.Begin(PrimitiveType.Quads);
                            GL.Color4(colors[colors.Length - i - 1].R, colors[colors.Length - i - 1].G, colors[colors.Length - i - 1].B, (byte)128);

                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);

                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);

                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);

                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);

                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);

                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);

                            GL.End();

                            GL.Disable(EnableCap.Lighting);
                            GL.Disable(EnableCap.DepthTest);
                            GL.LineWidth(2);
                            GL.Begin(PrimitiveType.Lines);
                            for (int k = 0; k < j.Instances.Length; ++k)
                            {
                                Instance inst = FileController.GetInstance(j.Parent.Parent.ID, j.Instances[k]);
                                GL.Vertex3(0, 0, 0);
                                GL.Vertex3(-inst.Pos.X + j.Coords[1].X, inst.Pos.Y - j.Coords[1].Y, inst.Pos.Z - j.Coords[1].Z);
                            }
                            GL.End();
                            GL.LineWidth(1);

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.End();

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.End();

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.End();

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.End();

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, -j.Coords[2].Y, j.Coords[2].Z);
                            GL.End();

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, -j.Coords[2].Z);
                            GL.Vertex3(j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.Vertex3(-j.Coords[2].X, j.Coords[2].Y, j.Coords[2].Z);
                            GL.End();
                            
                            DrawAxes(0, 0, 0, Math.Min(j.Coords[2].X, Math.Min(j.Coords[2].Y, j.Coords[2].Z)) / 2);
                            GL.Enable(EnableCap.DepthTest);
                            GL.Enable(EnableCap.Lighting);

                            GL.PopMatrix();
                        }
                    }
                }
            }

        }

        protected override bool IsInputKey(Keys keyData)
        {
            base.IsInputKey(keyData);
            switch (keyData)
            {
                case Keys.C:
                case Keys.T:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.C:
                    show_col_nodes = !show_col_nodes;
                    break;
                case Keys.T:
                    show_triggers = !show_triggers;
                    break;
            }
        }

        private void DrawAxes(float x, float y, float z, float size)
        {
            GL.PushMatrix();
            GL.Translate(x, y, z);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(1f, 0f, 0f);
            float new_ind_size = indicator_size * size;
            GL.Vertex3(+new_ind_size, 0, 0);
            GL.Vertex3(-new_ind_size, 0, 0);
            GL.Color3(0f, 1f, 0f);
            GL.Vertex3(0, +new_ind_size, 0);
            GL.Vertex3(0, -new_ind_size, 0);
            GL.Color3(0f, 0f, 1f);
            GL.Vertex3(0, 0, +new_ind_size);
            GL.Vertex3(0, 0, -new_ind_size);
            GL.End();
            GL.PopMatrix();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (dlist_col != -1)
            {
                GL.DeleteLists(dlist_col, 1);
                dlist_col = -1;
            }
            if (dlist_trg != -1)
            {
                GL.DeleteLists(dlist_trg, 1);
                dlist_trg = -1;
            }
            for (int i = 0; i < dlist_inst.Length; ++i)
                if (dlist_inst[i] != -1)
                {
                    GL.DeleteLists(dlist_inst[i], 1);
                    dlist_inst[i] = -1;
                }
            base.Dispose(disposing);
        }
    }
}
