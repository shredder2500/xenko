﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Threading.Tasks;
using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Input;

namespace SiliconStudio.Xenko.Graphics.Tests
{

    /// <summary>
    /// Test a sprite font imported from an external ttf file (not system font).
    /// </summary>
    [TestFixture]
    public class TestExternSpriteFont : GraphicTestGameBase
    {
        private SpriteBatch spriteBatch;
        private SpriteFont testFont;
        private Texture colorTexture;

        public TestExternSpriteFont()
        {
            CurrentVersion = 5; // Font names & sizes changed slightly
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawText).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            testFont = Content.Load<SpriteFont>("StaticFonts/ExternFont");

            // Instantiate a SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);
            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if(!ScreenShotAutomationEnabled)
                DrawText();
        }

        private void DrawText()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Render the text
            spriteBatch.Begin(GraphicsContext);

            const string text = "This is a font created from an external font file.";
            var dim = testFont.MeasureString(text);

            const int x = 20;
            const int y = 20;
            spriteBatch.Draw(colorTexture, new Rectangle(x, y, (int)dim.X, (int)dim.Y), Color.Green);
            spriteBatch.DrawString(testFont, text, new Vector2(x, y), Color.White);
            spriteBatch.DrawString(testFont, text, new Vector2(x, y + dim.Y + 20), Color.Red);

            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.S))
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "sprite-font-extern-test.png");
        }

        public static void Main()
        {
            using (var game = new TestExternSpriteFont())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunExternSpriteFont()
        {
            RunGameTest(new TestExternSpriteFont());
        }
    }
}