using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Game2048
{
    class Program : GameWindow
    {
        private int[,] grid = new int[4, 4];
        private Random random = new Random();
        private int score = 0;
        private bool gameOver = false;
        private bool won = false;

        // Кеш для текстур чисел та повідомлень
        private Dictionary<int, int> tileTextures = new Dictionary<int, int>();
        private int scoreTexture = 0;
        private int messageTexture = 0;
        private string currentMessage = "";

        public Program() : base(400, 450, GraphicsMode.Default, "2048 Game")
        {
            InitializeGame();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(0.95f, 0.9f, 0.85f, 1.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, 400, 0, 450, -1, 1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
        }

        private void InitializeGame()
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    grid[i, j] = 0;

            score = 0;
            gameOver = false;
            won = false;
            currentMessage = "";
            AddRandomTile();
            AddRandomTile();
            UpdateScoreTexture();
        }

        private void AddRandomTile()
        {
            var emptyCells = new List<(int, int)>();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (grid[i, j] == 0)
                        emptyCells.Add((i, j));

            if (emptyCells.Count > 0)
            {
                var (row, col) = emptyCells[random.Next(emptyCells.Count)];
                grid[row, col] = random.NextDouble() < 0.9 ? 2 : 4;
            }
        }

        // Метод для створення текстури з тексту
        private int CreateTextTexture(string text, Color bgColor, Color textColor, int width, int height, int fontSize)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(bgColor);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(text, font, new SolidBrush(textColor), new Rectangle(0, 0, width, height), sf);
                }
            }

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int texId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            bmp.UnlockBits(data);
            bmp.Dispose();
            return texId;
        }

        private void UpdateScoreTexture()
        {
            if (scoreTexture != 0) GL.DeleteTexture(scoreTexture);
            scoreTexture = CreateTextTexture("Score: " + score, Color.FromArgb(100, 90, 80), Color.White, 360, 30, 16);
        }

        private int GetTileTexture(int value)
        {
            if (!tileTextures.ContainsKey(value))
            {
                Color bgColor = GetTileColor(value);
                Color textColor = value <= 4 ? Color.FromArgb(120, 110, 100) : Color.White;
                int fontSize = value >= 1024 ? 24 : 32;
                tileTextures[value] = CreateTextTexture(value.ToString(), bgColor, textColor, 128, 128, fontSize);
            }
            return tileTextures[value];
        }

        private int GetMessageTexture(string msg, Color bgColor)
        {
            if (currentMessage == msg && messageTexture != 0) return messageTexture;
            if (messageTexture != 0) GL.DeleteTexture(messageTexture);
            messageTexture = CreateTextTexture(msg, bgColor, Color.White, 360, 60, 28);  
            currentMessage = msg;
            return messageTexture;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            float cellSize = 80f;
            float spacing = 10f;
            float startX = 20f;
            float startY = 60f;

            // 1. Малюємо рахунок зверху
            DrawTexturedQuad(20, 415, 360, 30, scoreTexture);

            // 2. Малюємо сітку та плитки
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float x = startX + j * (cellSize + spacing);
                    float y = startY + i * (cellSize + spacing);

                    // Порожня клітинка (сіра)
                    GL.Color3(0.8f, 0.75f, 0.7f);
                    DrawQuad(x, y, cellSize, cellSize);

                    // Плитка з числом
                    if (grid[i, j] != 0)
                    {
                        int texId = GetTileTexture(grid[i, j]);
                        DrawTexturedQuad(x + 5, y + 5, cellSize - 10, cellSize - 10, texId);
                    }
                }
            }

            // 3. Малюємо повідомлення Game Over або You Win!
            if (gameOver)
            {
                DrawTexturedQuad(20, 195, 360, 60, GetMessageTexture("GAME OVER", Color.FromArgb(150, 50, 50)));
            }
            else if (won)
            {
                DrawTexturedQuad(20, 195, 360, 60, GetMessageTexture("YOU WIN!", Color.FromArgb(50, 150, 50)));
            }

            SwapBuffers();
        }

        private void DrawQuad(float x, float y, float w, float h)
        {
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex2(x, y);
            GL.Vertex2(x + w, y);
            GL.Vertex2(x + w, y + h);
            GL.Vertex2(x, y + h);
            GL.End();
        }

        private void DrawTexturedQuad(float x, float y, float w, float h, int texId)
        {
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, texId);
            GL.Color3(1.0f, 1.0f, 1.0f);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 1); GL.Vertex2(x, y);       
            GL.TexCoord2(1, 1); GL.Vertex2(x + w, y);   
            GL.TexCoord2(1, 0); GL.Vertex2(x + w, y + h); 
            GL.TexCoord2(0, 0); GL.Vertex2(x, y + h);   
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }

        private Color GetTileColor(int value)
        {
            switch (value)
            {
                case 2: return Color.FromArgb(238, 228, 218);
                case 4: return Color.FromArgb(237, 224, 200);
                case 8: return Color.FromArgb(242, 177, 121);
                case 16: return Color.FromArgb(245, 149, 99);
                case 32: return Color.FromArgb(246, 124, 95);
                case 64: return Color.FromArgb(246, 94, 59);
                case 128: return Color.FromArgb(237, 207, 114);
                case 256: return Color.FromArgb(237, 204, 97);
                case 512: return Color.FromArgb(237, 200, 80);
                case 1024: return Color.FromArgb(237, 197, 63);
                case 2048: return Color.FromArgb(237, 194, 46);
                default: return Color.FromArgb(100, 100, 100);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            var keyboard = OpenTK.Input.Keyboard.GetState();

            if (keyboard.IsKeyDown(Key.Escape)) { Exit(); return; }

            if (gameOver || won)
            {
                if (keyboard.IsKeyDown(Key.R)) InitializeGame();
                return;
            }

            bool moved = false;
            if (keyboard.IsKeyDown(Key.Up)) moved = MoveUp();
            else if (keyboard.IsKeyDown(Key.Down)) moved = MoveDown();
            else if (keyboard.IsKeyDown(Key.Left)) moved = MoveLeft();
            else if (keyboard.IsKeyDown(Key.Right)) moved = MoveRight();

            if (moved)
            {
                AddRandomTile();
                UpdateScoreTexture(); // Оновлюємо текстуру рахунку
                CheckWin();
                CheckGameOver();
            }
        }

        private bool MoveLeft()
        {
            bool moved = false;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (grid[i, j] != 0)
                    {
                        int col = j;
                        while (col > 0 && grid[i, col - 1] == 0) { grid[i, col - 1] = grid[i, col]; grid[i, col] = 0; col--; moved = true; }
                        if (col > 0 && grid[i, col - 1] == grid[i, col]) { grid[i, col - 1] *= 2; score += grid[i, col - 1]; grid[i, col] = 0; moved = true; }
                    }
                }
            }
            return moved;
        }

        private bool MoveRight()
        {
            bool moved = false;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 3; j >= 0; j--)
                {
                    if (grid[i, j] != 0)
                    {
                        int col = j;
                        while (col < 3 && grid[i, col + 1] == 0) { grid[i, col + 1] = grid[i, col]; grid[i, col] = 0; col++; moved = true; }
                        if (col < 3 && grid[i, col + 1] == grid[i, col]) { grid[i, col + 1] *= 2; score += grid[i, col + 1]; grid[i, col] = 0; moved = true; }
                    }
                }
            }
            return moved;
        }

        private bool MoveUp()
        {
            bool moved = false;
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (grid[i, j] != 0)
                    {
                        int row = i;
                        while (row > 0 && grid[row - 1, j] == 0) { grid[row - 1, j] = grid[row, j]; grid[row, j] = 0; row--; moved = true; }
                        if (row > 0 && grid[row - 1, j] == grid[row, j]) { grid[row - 1, j] *= 2; score += grid[row - 1, j]; grid[row, j] = 0; moved = true; }
                    }
                }
            }
            return moved;
        }

        private bool MoveDown()
        {
            bool moved = false;
            for (int j = 0; j < 4; j++)
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (grid[i, j] != 0)
                    {
                        int row = i;
                        while (row < 3 && grid[row + 1, j] == 0) { grid[row + 1, j] = grid[row, j]; grid[row, j] = 0; row++; moved = true; }
                        if (row < 3 && grid[row + 1, j] == grid[row, j]) { grid[row + 1, j] *= 2; score += grid[row + 1, j]; grid[row, j] = 0; moved = true; }
                    }
                }
            }
            return moved;
        }

        private void CheckWin()
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (grid[i, j] == 2048) won = true;
        }

        private void CheckGameOver()
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (grid[i, j] == 0) return;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    if (j < 3 && grid[i, j] == grid[i, j + 1]) return;
                    if (i < 3 && grid[i, j] == grid[i + 1, j]) return;
                }
            gameOver = true;
        }

        public static void Main(string[] args)
        {
            using (var game = new Program()) { game.Run(30.0); }
        }
    }
}