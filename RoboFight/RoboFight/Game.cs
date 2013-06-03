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
        ParticleManager particleManager = new ParticleManager();
        ParallaxManager parallaxManager;
        HUD gameHud;

        Texture2D skyTex;
        Texture2D titleTex;
        SpriteFont font;

        static Random rand = new Random();

        List<int> levelSectors = new List<int>();
        List<Rectangle> triggerRects = new List<Rectangle>();
        List<Color> sectorColors = new List<Color>();

        Dictionary<int, MapObjectLayer> WalkableLayers = new Dictionary<int, MapObjectLayer>();

        bool inTrigger = false;

        float travelledDist = 0f;

        KeyboardState lks;
        GamePadState lgs;

        Color currentSectorColor;

        bool showingTitleScreen = false;
        bool titleScreenExiting = false;
        float titleAlpha = 0f;

        int highScore = 0;
        int prevScore = 0;

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

            AudioController.LoadContent(Content);

            gameMap = Content.Load<Map>("testmap");

            enemyManager.LoadContent(Content, GraphicsDevice);
            itemManager.LoadContent(Content, GraphicsDevice);
            projectileManager.LoadContent(Content);
            particleManager.LoadContent(Content);
            gameHud = new HUD(GraphicsDevice.Viewport);
            gameHud.LoadContent(Content);

            parallaxManager = new ParallaxManager(GraphicsDevice.Viewport);
            parallaxManager.Layers.Add(new ParallaxLayer(Content.Load<Texture2D>("bg/bg0"), new Vector2(0, 0), 0.25f, false, Color.White * 0.75f));
            parallaxManager.Layers.Add(new ParallaxLayer(Content.Load<Texture2D>("bg/bg1"), new Vector2(0, 0), 0.15f, false, Color.White * 0.75f));

            for (int i = 0; i < NUM_SECTORS; i++)
            {
                WalkableLayers.Add(i, gameMap.GetLayer("Walkable" + i) as MapObjectLayer);
            }

            gameHero = new Robot(Helper.PtoV((gameMap.GetLayer("Spawn") as MapObjectLayer).Objects[0].Location.Center), true);
            gameHero.LoadContent(Content, GraphicsDevice);

            gameCamera = new Camera(GraphicsDevice.Viewport, gameMap);

            skyTex = Content.Load<Texture2D>("sky");
            titleTex = Content.Load<Texture2D>("title");

            font = Content.Load<SpriteFont>("font");

            ResetGame();

            currentSectorColor = sectorColors[0];

            showingTitleScreen = true;
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
                GamePadState gs = GamePad.GetState(PlayerIndex.One);

                if (showingTitleScreen)
                {
                    if (titleScreenExiting)
                    {
                        if (titleAlpha > 0f)
                        {
                            titleAlpha -= 0.01f;
                        }
                        else
                        {
                            titleScreenExiting = false;
                            showingTitleScreen = false;
                            AudioController.PlayMusic("theme");
                        }
                    }
                    else
                        if (titleAlpha < 1f) titleAlpha += 0.01f;
                }

                if (!showingTitleScreen)
                {

                   
                        if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A) || gs.IsButtonDown(Buttons.LeftThumbstickLeft)) gameHero.MoveLeftRight(-1f);
                        else if (ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D) || gs.IsButtonDown(Buttons.LeftThumbstickRight)) gameHero.MoveLeftRight(1f);

                        if (ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W) || gs.ThumbSticks.Left.Y>0.1f) gameHero.MoveUpDown(-1f);
                        if (ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S) || gs.ThumbSticks.Left.Y<-0.1f) gameHero.MoveUpDown(1f);

                        if (ks.IsKeyDown(Keys.Space) || gs.IsButtonDown(Buttons.A)) gameHero.Jump();

                        if ((ks.IsKeyDown(Keys.X) || ks.IsKeyDown(Keys.RightShift) || gs.IsButtonDown(Buttons.B)) && !(lks.IsKeyDown(Keys.X) || lks.IsKeyDown(Keys.RightShift) || lgs.IsButtonDown(Buttons.B))) gameHero.Pickup();

                        gameHero.Attack((ks.IsKeyDown(Keys.Z) || ks.IsKeyDown(Keys.Enter) || gs.IsButtonDown(Buttons.X)));

                        lks = ks;
                        lgs = gs;

                        gameHero.Update(gameTime, gameCamera, gameMap, levelSectors, WalkableLayers, gameHero);

                        gameCamera.Target = gameHero.Position;
                        gameCamera.Update(GraphicsDevice.Viewport.Bounds);

                        if (!inTrigger && gameHero.Position.X - gameCamera.Width > travelledDist)
                        {
                           
                            travelledDist = gameHero.Position.X - gameCamera.Width;
                           
                        }                   

                        gameCamera.ClampRect.X = (int)travelledDist;
                        gameCamera.ClampRect.Width = ((gameMap.Width * gameMap.TileWidth) * levelSectors.Count());

                        float colLerp = (1f / (gameMap.Width * gameMap.TileWidth)) * ((travelledDist + gameCamera.Width) - (gameHero.Sector * (gameMap.Width * gameMap.TileWidth)));
                        currentSectorColor = Color.Lerp(sectorColors[gameHero.Sector], sectorColors[gameHero.Sector + 1], colLerp);

                        //foreach (ParallaxLayer l in parallaxManager.Layers) l.Tint = currentSectorColor;
                        parallaxManager.Layers[0].Tint = currentSectorColor * 0.5f;
                        parallaxManager.Layers[1].Tint = currentSectorColor * 0.75f;

                        gameHero.Position = Vector2.Clamp(gameHero.Position, gameCamera.Position - (new Vector2(gameCamera.Width, gameCamera.Height) / 2), gameCamera.Position + (new Vector2(gameCamera.Width, gameCamera.Height) / 2));

                        enemyManager.Update(gameTime, gameCamera, gameMap, levelSectors, WalkableLayers, gameHero);
                        itemManager.Update(gameTime, gameCamera, gameMap, levelSectors, WalkableLayers, gameHero);
                        projectileManager.Update(gameTime, gameHero);
                        particleManager.Update(gameTime, gameHero, gameMap, levelSectors, WalkableLayers);

                        if (levelSectors.Count - gameHero.Sector < 3)
                        {
                            GenerateSector();
                            //if (gameHero.Sector > 3)
                            //{
                            //    levelSectors.RemoveAt(gameHero.Sector - 3);
                            //}
                        }

                        CheckTriggers();

                        if (inTrigger)
                        {
                            if (enemyManager.Enemies.Count == 0)
                            {
                                inTrigger = false;
                                gameCamera.Holding = false;
                            }
                        }

                        gameHud.Update(gameTime, gameHero);

                        parallaxManager.Update(gameTime, gameCamera.Position);

                        if (gameHero.Dead && !gameHero.Active && !showingTitleScreen)
                        {
                            prevScore = gameHero.Score;
                            if (gameHero.Score > highScore) highScore = gameHero.Score;
                            showingTitleScreen = true;
                            foreach (Robot r in enemyManager.Enemies) r.fistSound.Stop();
                            AudioController.StopMusic();
                        }
                   
                }
                else
                {
                    if (!titleScreenExiting && titleAlpha>=1f && (ks.IsKeyDown(Keys.Space) || gs.IsButtonDown(Buttons.A) || ks.IsKeyDown(Keys.X) || ks.IsKeyDown(Keys.RightShift) || gs.IsButtonDown(Buttons.B) || gs.IsButtonDown(Buttons.Start) || ks.IsKeyDown(Keys.Enter)))
                    {
                        titleScreenExiting = true;

                        ResetGame();
                    }

                    
                }

               
            }

            AudioController.Update(gameTime);

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

            if (!showingTitleScreen)
            {

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, Matrix.CreateTranslation(-(int)gameCamera.Position.X, -(int)gameCamera.Position.Y, 0) * Matrix.CreateRotationZ(-gameCamera.Rotation) * Matrix.CreateTranslation(gameCamera.Width / 2, gameCamera.Height / 2, 0));
                parallaxManager.Draw(spriteBatch);
                spriteBatch.End();

                float x = 0;

                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix * Matrix.CreateTranslation(new Vector3(0f, 0f, 0f)));

                x = 0;
                foreach (int sec in levelSectors)
                {
                    gameMap.DrawLayer(spriteBatch, "Tile" + sec.ToString(), gameCamera, currentSectorColor, false, 1f, new Vector2((gameMap.Width * gameMap.TileWidth) * x, 0));
                    x++;
                }

                spriteBatch.End();

                particleManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
                itemManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
                enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
                projectileManager.Draw(GraphicsDevice, spriteBatch, gameCamera, 0f, gameHero.Position.Y);
                gameHero.Draw(GraphicsDevice, spriteBatch, gameCamera);
                particleManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);
                itemManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);
                enemyManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);
                projectileManager.Draw(GraphicsDevice, spriteBatch, gameCamera, gameHero.Position.Y, gameCamera.ClampRect.Height);


                spriteBatch.Begin();
                gameHud.Draw(spriteBatch);
                spriteBatch.End();
            }
            else
            {

                spriteBatch.Begin();
                spriteBatch.Draw(titleTex, new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) / 2, null, Color.White * titleAlpha, 0f, new Vector2(titleTex.Width / 2, titleTex.Height / 2), 1f, SpriteEffects.None, 1);

                Helper.ShadowText(spriteBatch, font, "WASD/Cursors/Left Stick: Move    Space/Gamepad A: Jump    X/Right Shift/Gamepad B: Pick up/Drop", new Vector2(GraphicsDevice.Viewport.Width / 2, 20), Color.White * titleAlpha, font.MeasureString("WASD/Cursors/Left Stick: Move    Space/Gamepad A: Jump    X/Right Shift/Gamepad B: Pick up/Drop") / 2, 0.5f);
                Helper.ShadowText(spriteBatch, font, "Z/Enter/Gamepad X: Attack - Hold to charge punch attack (no weapon)", new Vector2(GraphicsDevice.Viewport.Width / 2, 40), Color.White * titleAlpha, font.MeasureString("Z/Enter/Gamepad X: Attack - Hold to charge punch attack (no weapon)") / 2, 0.5f);
                Helper.ShadowText(spriteBatch, font, "By Gareth Williams (@garethiw) - One Game a Month May 2013", new Vector2(GraphicsDevice.Viewport.Width / 2, 80), Color.LightGray * titleAlpha, font.MeasureString("By Gareth Williams (@garethiw) - One Game a Month May 2013") / 2, 0.5f);
                Helper.ShadowText(spriteBatch, font, "High Score: " + highScore.ToString(), new Vector2(20, GraphicsDevice.Viewport.Height - 50), Color.White * titleAlpha, Vector2.Zero, 0.75f);
                Helper.ShadowText(spriteBatch, font, "Previous Score: " + prevScore.ToString(), new Vector2(GraphicsDevice.Viewport.Width - 20 - (font.MeasureString("Previous Score: " + prevScore.ToString()).X *0.75f), GraphicsDevice.Viewport.Height - 50), Color.White * titleAlpha, Vector2.Zero, 0.75f);

                //
                spriteBatch.End();
            }
           

            base.Draw(gameTime);
        }

        void ResetGame()
        {
            travelledDist = 0;

            levelSectors.Clear();
            triggerRects.Clear();

            for (int i = 0; i < 3; i++)
                GenerateSector();

            gameHero.Reset(Helper.PtoV((gameMap.GetLayer("Spawn") as MapObjectLayer).Objects[0].Location.Center));

            gameCamera = new Camera(GraphicsDevice.Viewport, gameMap);
            gameCamera.Position = gameHero.Position;
            gameCamera.Target = gameHero.Position;
            gameCamera.Holding = false;
            gameCamera.HoldingPosition = gameCamera.Position;

            inTrigger = false;

            foreach (Robot r in enemyManager.Enemies)
            {
                r.Dead = true;
                r.Active = false;
            }
            enemyManager.largestNumberSpawned = 0;
            enemyManager.spawnsWithoutWeapon = 0;

            particleManager.Reset();

            itemManager.Items.Clear();
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
