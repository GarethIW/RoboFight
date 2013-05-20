using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using TiledLib;

namespace RoboFight
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        const int NUM_SECTORS = 2;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Map gameMap;
        Camera gameCamera;
        Hero gameHero;

        Texture2D skyTex;

        static Random rand = new Random();

        List<int> levelSectors = new List<int>();
        List<Rectangle> triggerRects = new List<Rectangle>();

        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Window.AllowUserResizing = false;

            graphics.ApplyChanges();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            gameMap = Content.Load<Map>("testmap");

            gameHero = new Hero(Helper.PtoV((gameMap.GetLayer("Spawn") as MapObjectLayer).Objects[0].Location.Center));
            gameHero.LoadContent(Content, GraphicsDevice);

            gameCamera = new Camera(GraphicsDevice.Viewport, gameMap);
            gameCamera.Position = gameHero.Position;
            gameCamera.Target = gameHero.Position;

            skyTex = Content.Load<Texture2D>("sky");

            for (int i = 0; i < 3; i++)
                GenerateSector();

            
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (IsActive)
            {

                KeyboardState ks = Keyboard.GetState();

                if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A)) gameHero.MoveLeftRight(-1f);
                else if (ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D)) gameHero.MoveLeftRight(1f);

                if (ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W)) gameHero.MoveUpDown(-1f);
                if (ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S)) gameHero.MoveUpDown(1f);

                if (ks.IsKeyDown(Keys.Space)) gameHero.Jump();

                //if (ks.IsKeyDown(Keys.F10) && !lks.IsKeyDown(Keys.F10))
                //{

                //    graphics.ToggleFullScreen();
                //    if (graphics.IsFullScreen)
                //    {
                //        graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
                //        graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
                //    }
                //    else
                //    {
                //        graphics.PreferredBackBufferWidth = 1280;
                //        graphics.PreferredBackBufferHeight = 720;
                //    }
                //    graphics.ApplyChanges();
                //}

                gameHero.Update(gameTime, gameCamera, gameMap, levelSectors);

                gameCamera.Target = gameHero.Position;
                gameCamera.Update(GraphicsDevice.Viewport.Bounds);

                gameCamera.ClampRect.Width = (gameMap.Width * gameMap.TileWidth) * levelSectors.Count();

                if (levelSectors.Count - gameHero.Sector < 2)
                    GenerateSector();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();
            spriteBatch.Draw(skyTex, GraphicsDevice.Viewport.Bounds, Color.White);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix * Matrix.CreateTranslation(new Vector3(0f, 0f, 0f)));

            int x=0;
            foreach(int sec in levelSectors)
            {
                gameMap.DrawLayer(spriteBatch, "Tile" + sec.ToString(), gameCamera, Color.White, false, 1f, new Vector2((gameMap.Width * gameMap.TileWidth) * x,0));
                x++;
            }

            

            spriteBatch.End();

            gameHero.Draw(GraphicsDevice, spriteBatch, gameCamera);

            base.Draw(gameTime);
        }

        void GenerateSector()
        {
            int sect = rand.Next(NUM_SECTORS);
            levelSectors.Add(sect);

            MapObjectLayer triggerLayer = gameMap.GetLayer("Triggers" + sect.ToString()) as MapObjectLayer;

            for (int i = 0; i < 3; i++)
            {
                Rectangle trig = triggerLayer.Objects[rand.Next(triggerLayer.Objects.Count)].Location;
                trig.Offset((gameMap.TileWidth * gameMap.Width) * (levelSectors.Count-1),0);

                bool found = true;
                while(found)
                {
                    found = false;
                    foreach (Rectangle r in triggerRects)
                        if (r == trig)
                        {
                            trig = triggerLayer.Objects[rand.Next(triggerLayer.Objects.Count)].Location;
                            trig.Offset((gameMap.TileWidth * gameMap.Width) * (levelSectors.Count - 1), 0);
                            found = true;
                            break;
                        }
                }

                triggerRects.Add(trig);
            }
        }
    }
}
