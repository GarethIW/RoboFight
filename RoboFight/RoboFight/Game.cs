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
        const int NUM_SECTORS = 5;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Map gameMap;
        Camera gameCamera;
        Robot gameHero;
        EnemyManager enemyManager = new EnemyManager();
        ItemManager itemManager = new ItemManager();
        ProjectileManager projectileManager = new ProjectileManager();

        Texture2D skyTex;

        static Random rand = new Random();

        List<int> levelSectors = new List<int>();
        List<Rectangle> triggerRects = new List<Rectangle>();
        List<Color> sectorColors = new List<Color>();

        Dictionary<int, MapObjectLayer> WalkableLayers = new Dictionary<int, MapObjectLayer>();

        bool inTrigger = false;

        float travelledDist = 0f;

        KeyboardState lks;

        Color currentSectorColor;

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

            enemyManager.LoadContent(Content, GraphicsDevice);
            itemManager.LoadContent(Content, GraphicsDevice);
            projectileManager.LoadContent(Content);

            gameHero = new Robot(Helper.PtoV((gameMap.GetLayer("Spawn") as MapObjectLayer).Objects[0].Location.Center), true);
            gameHero.LoadContent(Content, GraphicsDevice);

            gameCamera = new Camera(GraphicsDevice.Viewport, gameMap);
            gameCamera.Position = gameHero.Position;
            gameCamera.Target = gameHero.Position;

            

            for (int i = 0; i < NUM_SECTORS; i++)
            {
                WalkableLayers.Add(i, gameMap.GetLayer("Walkable" + i) as MapObjectLayer);
            }

            skyTex = Content.Load<Texture2D>("sky");

            for (int i = 0; i < 3; i++)
                GenerateSector();

            currentSectorColor = sectorColors[0];
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

                if ((ks.IsKeyDown(Keys.X) || ks.IsKeyDown(Keys.RightShift)) && !(lks.IsKeyDown(Keys.X) || lks.IsKeyDown(Keys.RightShift))) gameHero.Pickup();

                gameHero.Attack((ks.IsKeyDown(Keys.Z) || ks.IsKeyDown(Keys.Enter)));

                lks = ks;

                

                gameHero.Update(gameTime, gameCamera, gameMap, levelSectors, WalkableLayers, gameHero);

                gameCamera.Target = gameHero.Position;
                gameCamera.Update(GraphicsDevice.Viewport.Bounds);

                if (!inTrigger && gameHero.Position.X - gameCamera.Width > travelledDist) travelledDist = gameHero.Position.X - gameCamera.Width;
                gameCamera.ClampRect.X = (int)travelledDist;
                gameCamera.ClampRect.Width = ((gameMap.Width * gameMap.TileWidth) * levelSectors.Count());

                float colLerp = (1f / (gameMap.Width * gameMap.TileWidth)) * ((travelledDist + gameCamera.Width) - (gameHero.Sector * (gameMap.Width * gameMap.TileWidth)));
                currentSectorColor = Color.Lerp(sectorColors[gameHero.Sector], sectorColors[gameHero.Sector + 1], colLerp);

                gameHero.Position = Vector2.Clamp(gameHero.Position, gameCamera.Position - (new Vector2(gameCamera.Width, gameCamera.Height) / 2), gameCamera.Position + (new Vector2(gameCamera.Width, gameCamera.Height) / 2));

                enemyManager.Update(gameTime, gameCamera, gameMap, levelSectors, WalkableLayers, gameHero);
                itemManager.Update(gameTime, gameCamera, gameMap, levelSectors, WalkableLayers, gameHero);
                projectileManager.Update(gameTime, gameHero);

                if (levelSectors.Count - gameHero.Sector < 3)
                    GenerateSector();

                CheckTriggers();

                if (inTrigger)
                {
                    if (enemyManager.Enemies.Count == 0)
                    {
                        inTrigger = false;
                        gameCamera.Holding = false;
                    }
                }
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
                gameMap.DrawLayer(spriteBatch, "Tile" + sec.ToString(), gameCamera, currentSectorColor, false, 1f, new Vector2((gameMap.Width * gameMap.TileWidth) * x,0));
                x++;
            }

            spriteBatch.End();

            itemManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
            enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
            projectileManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
            gameHero.Draw(GraphicsDevice, spriteBatch, gameCamera);
            itemManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);
            enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);
            projectileManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);


            base.Draw(gameTime);
        }

        void GenerateSector()
        {
            int sect = rand.Next(NUM_SECTORS);
            levelSectors.Add(sect);

            sectorColors.Add(new Color(0.5f + ((float)rand.NextDouble() * 0.5f), 0.5f + ((float)rand.NextDouble() * 0.5f), 0.5f + ((float)rand.NextDouble() * 0.5f)));

            MapObjectLayer triggerLayer = gameMap.GetLayer("Triggers" + sect.ToString()) as MapObjectLayer;

            for (int i = 0; i < 4; i++)
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

                //if(trig.X>(gameCamera.Width - (gameCamera.Width/3)))
                    triggerRects.Add(trig);
            }
        }

        void CheckTriggers()
        {
            if (inTrigger) return; 

            Rectangle? trig = null;
            foreach (Rectangle r in triggerRects)
            {
                if (r.Contains(Helper.VtoP(gameHero.Position)))
                {
                    trig = r;

                    if (enemyManager.Spawn(gameMap, gameCamera, levelSectors, gameHero) > 0)
                    {
                        inTrigger = true;
                        gameCamera.Holding = true;
                        gameCamera.HoldingPosition = gameHero.Position;
                    }
                }
            }

            if(trig.HasValue)
                triggerRects.Remove(trig.Value);
        }
    }
}
