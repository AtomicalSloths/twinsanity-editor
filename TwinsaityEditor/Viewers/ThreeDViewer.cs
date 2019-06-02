﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace TwinsaityEditor
{
    public abstract class ThreeDViewer : GLControl
    {
        //add to preferences later
        protected static readonly Color[] colors = new[] { Color.Gray, Color.SlateGray, Color.DodgerBlue, Color.OrangeRed, Color.Red, Color.Pink, Color.LimeGreen, Color.DarkSlateBlue, Color.SaddleBrown, Color.LightSteelBlue, Color.SandyBrown, Color.Peru, Color.RoyalBlue, Color.DimGray, Color.Coral, Color.AliceBlue, Color.LightGray, Color.Cyan, Color.MediumTurquoise, Color.DarkSlateGray, Color.DarkSalmon, Color.DarkRed, Color.DarkCyan, Color.MediumVioletRed, Color.MediumOrchid, Color.DarkGray, Color.Yellow, Color.Goldenrod };
        protected static readonly float indicator_size = 0.5f;
        protected static Matrix3 identity_mat = Matrix3.Identity;

        protected VertexBufferData[] vtx;

        protected Dictionary<char, Vertex[]> charVtx = new Dictionary<char, Vertex[]>();
        private Dictionary<char, int> charVtxOffs = new Dictionary<char, int>();
        private int charVtxMax = 0, charVtxBuf, charVtxBufLen = 0;

        private Vector3 pos, rot, sca;
        private float range;
        private Timer refresh;
        private bool k_w, k_a, k_s, k_d, k_e, k_q, m_l;
        private int m_x, m_y;
        private EventHandler _inputHandle;
        private FontWrapper.FontService _fntService;
        private Dictionary<char, int> textureCharMap = new Dictionary<char, int>();
        private Dictionary<char, float> charAdvanceX = new Dictionary<char, float>();
        private Dictionary<char, float> charBearingX = new Dictionary<char, float>();
        private Dictionary<char, float> charBearingY = new Dictionary<char, float>();
        private Dictionary<char, float> charHeight = new Dictionary<char, float>();
        protected float size = 24f, zNear = 0.5f, zFar = 1500f;

        protected long timeRenderObj = 0, timeRenderObj_min = long.MaxValue, timeRenderObj_max = 0;
        protected long timeRenderHud = 0, timeRenderHud_min = long.MaxValue, timeRenderHud_max = 0;

        public ThreeDViewer()
        {
            _fntService = new FontWrapper.FontService();
            List<FileInfo> fonts = (List<FileInfo>)_fntService.GetFontFiles(new DirectoryInfo("Fonts/"), false);
            _fntService.SetFont(fonts[0].FullName);
            _fntService.SetSize(size);

            pos = new Vector3(0, 0, 0);
            rot = new Vector3(0, 0, 0);
            sca = new Vector3(1.0f, 1.0f, 1.0f);
            range = 100;

            _inputHandle = (sender, e) =>
            {
                if (e is MouseEventArgs)
                {
                    Invalidate();
                }
                else
                {
                    float speed = range / 250;
                    int v = 0, h = 0, d = 0;
                    if (k_w)
                        d++;
                    if (k_a)
                        h++;
                    if (k_s)
                        d--;
                    if (k_d)
                        h--;
                    if (k_e)
                        v++;
                    if (k_q)
                        v--;
                    Vector3 delta = new Vector3(h, v, d) * speed;
                    Matrix4 rot_matrix = Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), rot.X);
                    rot_matrix *= Matrix4.CreateFromAxisAngle(new Vector3(1, 0, 0), rot.Y);
                    rot_matrix *= Matrix4.CreateFromAxisAngle(new Vector3(0, 0, 1), rot.Z);

                    Vector3 fin_delta = new Vector3(rot_matrix * new Vector4(delta, 1.0f));

                    pos -= fin_delta;

                    if ((h | v | d) != 0)
                        Invalidate();
                }
            };

            refresh = new Timer
            {
                Interval = (int)Math.Floor(1.0/60*1000), //Set to ~60fps by default, TODO: Add to Preferences later
                Enabled = true
            };

            refresh.Tick += delegate (object sender, EventArgs e)
            {
                _inputHandle(sender, e);
                Invalidate();
            };

            ParentChanged += ThreeDViewer_ParentChanged;
        }

        private void ThreeDViewer_ParentChanged(object sender, EventArgs e)
        {
            Form par = (Form)Parent;
            par.Icon = Properties.Resources.icon;
            ParentChanged -= ThreeDViewer_ParentChanged;
        }

        protected abstract void RenderObjects();
        protected abstract void RenderHUD();

        private void ResetCamera()
        {
            pos = new Vector3(0, 0, 0);
            rot = new Vector3(0, 0, 0);

            timeRenderObj = 0; timeRenderObj_min = long.MaxValue; timeRenderObj_max = 0;
            timeRenderHud = 0; timeRenderHud_min = long.MaxValue; timeRenderHud_max = 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            switch (e.Button)
            {
                case MouseButtons.Left:
                    m_l = true;
                    break;
                //case MouseButtons.Right:
                //    m_r = true;
                //    break;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            switch (e.Button)
            {
                case MouseButtons.Left:
                    m_l = false;
                    break;
                //case MouseButtons.Right:
                //    m_r = false;
                //    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (m_l)
            {
                rot.X += (e.X - m_x) / 180.0f * MathHelper.Pi / (Size.Width / 480f);
                rot.Y += (e.Y - m_y) / 180.0f * MathHelper.Pi / (Size.Height / 480f);
                rot.X += rot.X > MathHelper.Pi ? -MathHelper.TwoPi : rot.X < -MathHelper.Pi ? MathHelper.TwoPi : 0;
                if (rot.Y > MathHelper.PiOver2)
                    rot.Y = MathHelper.PiOver2;
                if (rot.Y < -MathHelper.PiOver2)
                    rot.Y = -MathHelper.PiOver2;
            }
            m_x = e.X;
            m_y = e.Y;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            range -= e.Delta / 120 * 30;
            if (range > 500f)
                range = 500f;
            else if (range < 25f)
                range = 25f;
            zNear = range / 100F;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.W:
                case Keys.A:
                case Keys.S:
                case Keys.D:
                case Keys.Q:
                case Keys.E:
                case Keys.R:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.W:
                    k_w = true;
                    break;
                case Keys.A:
                    k_a = true;
                    break;
                case Keys.S:
                    k_s = true;
                    break;
                case Keys.D:
                    k_d = true;
                    break;
                case Keys.Q:
                    k_q = true;
                    break;
                case Keys.E:
                    k_e = true;
                    break;
                case Keys.R:
                    ResetCamera();
                    break;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            switch (e.KeyCode)
            {
                case Keys.W:
                    k_w = false;
                    break;
                case Keys.A:
                    k_a = false;
                    break;
                case Keys.S:
                    k_s = false;
                    break;
                case Keys.D:
                    k_d = false;
                    break;
                case Keys.Q:
                    k_q = false;
                    break;
                case Keys.E:
                    k_e = false;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            MakeCurrent();
            GL.Viewport(Location, Size);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver3, (float)Width/Height, zNear, zFar);
            GL.LoadMatrix(ref proj);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Scale(sca);
            GL.Rotate(MathHelper.RadiansToDegrees(rot.Y), 1, 0, 0);
            GL.Rotate(MathHelper.RadiansToDegrees(rot.X), 0, 1, 0);
            GL.Rotate(MathHelper.RadiansToDegrees(rot.Z), 0, 0, 1);
            Vector3 delta = new Vector3(0, 0, -1) * range / 25f;
            Matrix4 rot_matrix = Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), rot.X);
            rot_matrix *= Matrix4.CreateFromAxisAngle(new Vector3(1, 0, 0), rot.Y);
            rot_matrix *= Matrix4.CreateFromAxisAngle(new Vector3(0, 0, 1), rot.Z);

            Vector3 fin_delta = new Vector3(rot_matrix * new Vector4(delta, 1f));
            GL.Translate(-pos + fin_delta);
            GL.PushMatrix();
            GL.Translate(pos.X*2, 0, 0);
            GL.Scale(-1, 1, 1);
            DrawAxes(pos.X, pos.Y, pos.Z, 1);
            GL.PopMatrix();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            RenderObjects();
            RenderChars();
            watch.Stop();
            timeRenderObj = watch.ElapsedMilliseconds;
            timeRenderObj_max = Math.Max(timeRenderObj_max, timeRenderObj);
            timeRenderObj_min = Math.Min(timeRenderObj_min, timeRenderObj);
            watch = System.Diagnostics.Stopwatch.StartNew();
            DrawHUD();
            watch.Stop();
            timeRenderHud = watch.ElapsedMilliseconds;
            timeRenderHud_max = Math.Max(timeRenderHud_max, timeRenderHud);
            timeRenderHud_min = Math.Min(timeRenderHud_min, timeRenderHud);
            SwapBuffers();
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.AlphaFunc(AlphaFunction.Greater, 0);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.ClearColor(Color.MidnightBlue); //TODO: Add clear color to Preferences later
            GL.Enable(EnableCap.ColorMaterial);
            //GL.ShadeModel(ShadingModel.Flat); //TODO: Add to preferences
            GL.Light(LightName.Light0, LightParameter.Position, new float[] { 0, 0, 0, 1 });
            GL.LightModel(LightModelParameter.LightModelTwoSide, 1);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Normalize);
            GL.GenBuffers(1, out charVtxBuf);
            base.OnLoad(e);
        }

        protected void InitVBO(int count)
        {
            MakeCurrent();
            vtx = new VertexBufferData[count];
            for (int i = 0; i < count; ++i)
            {
                vtx[i] = new VertexBufferData();
            }
        }

        protected void UpdateVBO(int id)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vtx[id].ID);
            if (vtx[id].Vtx.Length > vtx[id].LastSize)
                GL.BufferData(BufferTarget.ArrayBuffer, Vertex.SizeOf * vtx[id].Vtx.Length, vtx[id].Vtx, BufferUsageHint.StaticDraw);
            else
                GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)0, Vertex.SizeOf * vtx[id].Vtx.Length, vtx[id].Vtx);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            vtx[id].LastSize = vtx[id].Vtx.Length;
        }

        protected void DrawAxes(float x, float y, float z, float size)
        {
            float new_ind_size = indicator_size * size;
            GL.PushMatrix();
            GL.Translate(x, y, z);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(1f, 0f, 0f);
            GL.Vertex3(+new_ind_size, 0, 0);
            GL.Vertex3(-new_ind_size / 2, 0, 0);
            GL.Color3(0f, 1f, 0f);
            GL.Vertex3(0, +new_ind_size, 0);
            GL.Vertex3(0, -new_ind_size / 2, 0);
            GL.Color3(0f, 0f, 1f);
            GL.Vertex3(0, 0, +new_ind_size);
            GL.Vertex3(0, 0, -new_ind_size / 2);
            GL.End();
            GL.PopMatrix();
        }

        private void RenderChars()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, charVtxBuf);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            foreach (var k in charVtx.Keys)
            {
                if (charVtxOffs[k] == 0) continue;
                if (charVtxBufLen < charVtx[k].Length)
                {
                    GL.BufferData(BufferTarget.ArrayBuffer, Vertex.SizeOf * charVtx[k].Length, charVtx[k], BufferUsageHint.DynamicDraw);
                    charVtxBufLen = charVtx[k].Length;
                }
                else
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)0, Vertex.SizeOf * charVtxOffs[k], charVtx[k]);
                }
                GL.BindTexture(TextureTarget.Texture2D, textureCharMap[k]);
                GL.VertexPointer(3, VertexPointerType.Float, Vertex.SizeOf, Vertex.OffsetOfPos);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, Vertex.SizeOf, Vertex.OffsetOfCol);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.SizeOf, Vertex.OffsetOfTex);
                GL.DrawArrays(PrimitiveType.Quads, 0, charVtxOffs[k]);
                charVtxOffs[k] = 0;
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void DrawHUD()
        {
            GL.DepthMask(false);
            GL.Disable(EnableCap.DepthTest);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Width, Height, 0, -1, 10);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            RenderHUD();
            GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);
        }

        protected int LoadTextTexture(ref Bitmap text, int quality = 0, bool flip_y = false)
        {
            if (flip_y)
                text.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GL.GenTextures(1, out int texture);

            GL.BindTexture(TextureTarget.Texture2D, texture);

            switch (quality)
            {
                case 0:
                default:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                    break;
                case 1:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                    break;
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToBorder);

            BitmapData data = text.LockBits(new Rectangle(0, 0, text.Width, text.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            text.UnlockBits(data);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texture;
        }

        protected void RenderString3DImmediate(string s)
        {
            float spacing = 2 / 3f;
            float x = (s.Length + 1) * (-spacing / 2f);
            foreach (char c in s)
            {
                x += spacing;
                if (c == ' ')
                    continue;
                if (!textureCharMap.ContainsKey(c))
                    GenCharTex(c);
                GL.BindTexture(TextureTarget.Texture2D, textureCharMap[c]);
                GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out float w);
                GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out float h);
                w /= size*2;
                h /= size;
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0, 1); GL.Vertex2(x-w, 0);
                GL.TexCoord2(1, 1); GL.Vertex2(x+w, 0);
                GL.TexCoord2(1, 0); GL.Vertex2(x+w, h);
                GL.TexCoord2(0, 0); GL.Vertex2(x-w, h);
                GL.End();
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected void RenderString3DToArray(string s, Color col, float x_off, float y_off, float z_off, ref Matrix3 rot_mat, float size_fac = 1F)
        {
            float spacing = 2 / 3f;
            float x = (s.Length + 1) * (-spacing / 2f);
            Vector3 off = new Vector3(x_off, y_off, z_off);
            foreach (char c in s)
            {
                x += spacing;
                if (c == ' ')
                    continue;
                if (!textureCharMap.ContainsKey(c))
                    GenCharTex(c);
                GL.BindTexture(TextureTarget.Texture2D, textureCharMap[c]);
                GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out float w);
                GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out float h);
                w /= size * 2;
                h /= size;
                if (!charVtx.ContainsKey(c))
                {
                    charVtx.Add(c, new Vertex[256]);
                    charVtxOffs.Add(c, 0);
                }
                else if (charVtxOffs[c] + 4 >= charVtx[c].Length)
                {
                    var arr = charVtx[c];
                    Array.Resize(ref arr, arr.Length * 2);
                    charVtx[c] = arr;
                    charVtxMax = Math.Max(charVtxMax, arr.Length);
                }
                charVtx[c][charVtxOffs[c]++] = new Vertex(new Vector3(x - w, 0, 0) * size_fac * rot_mat + off, new Vector2(0, 1), col);
                charVtx[c][charVtxOffs[c]++] = new Vertex(new Vector3(x + w, 0, 0) * size_fac * rot_mat + off, new Vector2(1, 1), col);
                charVtx[c][charVtxOffs[c]++] = new Vertex(new Vector3(x + w, h, 0) * size_fac * rot_mat + off, new Vector2(1, 0), col);
                charVtx[c][charVtxOffs[c]++] = new Vertex(new Vector3(x - w, h, 0) * size_fac * rot_mat + off, new Vector2(0, 0), col);
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected void RenderString2D(string s, float x, float y, float text_size, TextAnchor anchor = TextAnchor.TopLeft)
        {
            float text_size_fac = text_size / size;
            float start_x = x;
            if (anchor == TextAnchor.TopMiddle || anchor == TextAnchor.TopRight || anchor == TextAnchor.BotMiddle || anchor == TextAnchor.BotRight)
            {
                foreach (char c in s)
                {
                    if (!charAdvanceX.ContainsKey(c))
                        AddCharData(c);
                    float gAdvanceX = charAdvanceX[c] * text_size_fac;
                    float gBearingX = charBearingX[c] * text_size_fac;

                    x += gBearingX + gAdvanceX;
                }
                if (anchor == TextAnchor.BotMiddle || anchor == TextAnchor.TopMiddle)
                {
                    x = start_x - (x - start_x)/2;
                }
                else
                {
                    x = start_x - (x - start_x);
                }
            }
            foreach (char c in s)
            {
                if (!charAdvanceX.ContainsKey(c))
                    AddCharData(c);

                float gAdvanceX = charAdvanceX[c] * text_size_fac;
                float gBearingX = charBearingX[c] * text_size_fac;

                x += gBearingX;

                float glyphTop = (size - charBearingY[c]) * text_size_fac;

                if (c != ' ')
                {
                    if (!textureCharMap.ContainsKey(c))
                        GenCharTex(c);
                    GL.BindTexture(TextureTarget.Texture2D, textureCharMap[c]);
                    GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out float c_w);
                    GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out float c_h);
                    c_w *= text_size_fac;
                    c_h *= text_size_fac;
                    float y_hi = 0, y_lo = 0;
                    switch (anchor)
                    {
                        case TextAnchor.TopLeft:
                        case TextAnchor.TopMiddle:
                        case TextAnchor.TopRight:
                            y_hi = y + glyphTop;
                            y_lo = y_hi + c_h;
                            break;
                        case TextAnchor.BotLeft:
                        case TextAnchor.BotMiddle:
                        case TextAnchor.BotRight:
                            y_lo = y + (charHeight[c] - charBearingY[c]) * text_size_fac;
                            y_hi = y_lo - c_h;
                            break;
                    }
                    GL.Begin(PrimitiveType.Quads);
                    GL.TexCoord2(0, 1); GL.Vertex2(x, y_lo);
                    GL.TexCoord2(1, 1); GL.Vertex2(x + c_w, y_lo);
                    GL.TexCoord2(1, 0); GL.Vertex2(x + c_w, y_hi);
                    GL.TexCoord2(0, 0); GL.Vertex2(x, y_hi);
                    GL.End();
                }

                x += gAdvanceX;
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void GenCharTex(char c)
        {
            Bitmap bmp = _fntService.RenderString(c.ToString(), Color.White, Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF));
            textureCharMap.Add(c, LoadTextTexture(ref bmp));
            bmp.Dispose();
        }

        private void AddCharData(char c)
        {
            var face = _fntService.FontFace;
            face.LoadGlyph(face.GetCharIndex(c), SharpFont.LoadFlags.Default, SharpFont.LoadTarget.Normal);

            charAdvanceX.Add(c, (float)face.Glyph.Advance.X);
            charBearingX.Add(c, (float)face.Glyph.Metrics.HorizontalBearingX);
            charBearingY.Add(c, (float)face.Glyph.Metrics.HorizontalBearingY);
            charHeight.Add(c, (float)face.Glyph.Metrics.Height);
        }

        protected void SetPosition(Vector3 pos)
        {
            this.pos = pos;
        }

        protected override void Dispose(bool disposing)
        {
            refresh.Dispose();
            base.Dispose(disposing);
        }
    }
}
