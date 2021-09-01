using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace seaBattle_v2
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont defaultFont;

        //Текстурки
        private Texture2D userField;
        private Rectangle userFieldR;

        private Texture2D botField;
        private Rectangle botFieldR;

        private Texture2D lettersTexture;
        private Rectangle lettersTextureR_user;
        private Rectangle lettersTextureR_bot;

        private Texture2D numbersTexture;
        private Rectangle numbersTextureR_user;
        private Rectangle numbersTextureR_bot;
        
        private Texture2D[] ship = new Texture2D[4];
        private Rectangle[] shipStatic = new Rectangle[4];

        //служебное
        private int userScreenWidth;
        private int userScreenHeight;
        private int formWidth;
        private int formHeight;

        //логика игры
        byte[,] fieldTiles = new byte[10, 10];//прибавляем по одному с каждой стороны, чтобы избежать ошибок
        //будут задействованы лишь 1-10
        //0 - пусто, 1 - стоит начало корабля, 2 - стоит начало повернутого корабля
        byte[,] fieldTilesShips = new byte[10, 10];
        byte[] usedShips = new byte[4];
        byte[,] userMatrix = new byte[12, 12];
        byte[,] botMatrix = new byte[10, 10];
        byte usedShipsTotal = 0;
        //0 пусто, 1 стоит корабль, 2 подбит корабль, 3 промах по точке 
        //конец логики игры

        //управление
        private Vector2 mousePos;
        private Vector2 mousePosDebug;
        private Vector2 mouseDownPos;
        byte draggingItem; //0 - ничего, 1 - однопалубник, 2 - двух и тд
        int fieldTileSize;
        int pickedTileX, pickedTileY;
        bool isShipRotated;
        bool isEKeyPressed;

        public MouseState currentMouseState;

        bool startGameCheck;
        bool pressEnterCheck = true;
        string mousePosDebugString;
        bool isTimerOn;
        float timerCounter;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            userScreenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            userScreenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.PreferredBackBufferWidth = (int)(userScreenWidth * 0.315);
            graphics.PreferredBackBufferHeight = (int)(userScreenWidth * 0.2205);
            IsMouseVisible = true;
            //graphics.PreferredBackBufferWidth = (int)(userScreenWidth * 0.63);
            //graphics.PreferredBackBufferHeight = (int)(userScreenWidth * 0.441);
            formHeight = graphics.PreferredBackBufferHeight;
            formWidth = graphics.PreferredBackBufferWidth;
            for(int i = 0; i <= 9; i++)
            {
                for(int j = 0; j <= 9; j++)
                {
                    fieldTiles[i, j] = 0;
                }
            }
            usedShips[0] = 4;
            usedShips[1] = 3;
            usedShips[2] = 2;
            usedShips[3] = 1;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            userField = Content.Load<Texture2D>("field_1_noshade");
            userFieldR = new Rectangle((int)(formWidth * 0.06), (int)(formHeight * 0.33), (int)(formWidth * 0.38), (int)(formWidth * 0.38));
            fieldTileSize = userFieldR.Width / 10;

            botField = Content.Load<Texture2D>("field_2_noshade");
            botFieldR = new Rectangle((int)(formWidth * 0.566), (int)(formHeight * 0.33), (int)(formWidth * 0.38), (int)(formWidth * 0.38));

            lettersTexture = Content.Load<Texture2D>("letters");
            lettersTextureR_user = new Rectangle((int)(formWidth * 0.071), (int)(formHeight * 0.295), (int)(formWidth * 0.355), (int)(formHeight * 0.031));
            lettersTextureR_bot = new Rectangle((int)(formWidth * 0.575), (int)(formHeight * 0.295), (int)(formWidth * 0.355), (int)(formHeight * 0.031));

            numbersTexture = Content.Load<Texture2D>("numbers");
            numbersTextureR_user = new Rectangle((int)(formWidth * 0.026), (int)(formHeight * 0.352), (int)(formWidth * 0.027), (int)(formHeight * 0.505));
            numbersTextureR_bot = new Rectangle((int)(formWidth * 0.533), (int)(formHeight * 0.352), (int)(formWidth * 0.027), (int)(formHeight * 0.505));

            ship[3] = Content.Load<Texture2D>("ship_4");
            ship[2] = Content.Load<Texture2D>("ship_3");
            ship[1] = Content.Load<Texture2D>("ship_2");
            ship[0] = Content.Load<Texture2D>("ship_1");

            shipStatic[3] = new Rectangle((int)(formWidth * 0.04), (int)(formHeight * 0.03), (int)(formWidth * 0.146), (int)(formHeight * 0.052));
            shipStatic[2] = new Rectangle((int)(formWidth * 0.04), (int)(formHeight * 0.109), (int)(formWidth * 0.104), (int)(formHeight * 0.044));
            shipStatic[1] = new Rectangle((int)(formWidth * 0.04), (int)(formHeight * 0.188), (int)(formWidth * 0.069), (int)(formHeight * 0.04));
            shipStatic[0] = new Rectangle((int)(formWidth * 0.272), (int)(formHeight * 0.03), (int)(formWidth * 0.034), (int)(formHeight * 0.033));
            defaultFont = Content.Load<SpriteFont>("defaultfont");

        }

        protected override void UnloadContent() { }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();


            currentMouseState = Mouse.GetState();
            mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
            mousePosDebugString = mousePos.ToString();

            CheckLeftButtonDown();
            CheckLeftButtonReleased();
            CheckLeftButtonReleasedOutboard();

            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                startGameCheck = true;
                pressEnterCheck = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.E))
                if (!(isEKeyPressed))
                {
                    if (isShipRotated)
                        isShipRotated = false;
                    else
                        isShipRotated = true;
                    isEKeyPressed = true;
                }

            if (Keyboard.GetState().IsKeyUp(Keys.E))
                if (isEKeyPressed)
                {
                    isEKeyPressed = false;
                }

            base.Update(gameTime);

            if (isTimerOn)
            {
                timerCounter += gameTime.ElapsedGameTime.Milliseconds;
                if (timerCounter >= 1000)
                {
                    isTimerOn = false;
                    timerCounter = 0;
                }
            }
        }

        private bool CheckPlacementAbility(int X, int Y, byte shipType, bool shipRot)
        {
            if (shipRot)
            {
                if (Y + shipType > 11)
                    return false;
                for (int i = 0; i <= shipType - 1; i++)
                    if ((userMatrix[X, Y + i] == 1) || (userMatrix[X - 1, Y + i] == 1) || (userMatrix[X + 1, Y + i] == 1))
                        return false;
                if (userMatrix[X, Y + 1] == 1)
                    return false;
            }
            else
            {
                if (X + shipType > 11)
                    return false;
                for (int i = 0; i <= shipType - 1; i++)
                {
                    if ((userMatrix[X + i, Y] == 1) || (userMatrix[X + i, Y - 1] == 1) || (userMatrix[X + i, Y + 1] == 1))
                        return false;
                }
                if (userMatrix[X + 1, Y] == 1)
                    return false;
            }
            return true;
        }

        private void CheckLeftButtonDown()
        {
            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                mouseDownPos = mousePos;
                if (draggingItem == 0)
                    draggingItem = GetDraggingItem();
            }
        }

        private byte GetDraggingItem()
        {
            if ((mouseDownPos.X > shipStatic[3].X) && (mouseDownPos.X < (shipStatic[3].X + shipStatic[3].Width)) 
                && (mouseDownPos.Y > shipStatic[3].Y) && (mouseDownPos.Y < (shipStatic[3].Y + shipStatic[3].Height)))
            {
                if (usedShips[3] == 0)
                    return 0;
                else
                return 4;
            }
            else if ((mouseDownPos.X > shipStatic[2].X) && (mouseDownPos.X < (shipStatic[2].X + shipStatic[2].Width)) 
                && (mouseDownPos.Y > shipStatic[2].Y) && (mouseDownPos.Y < (shipStatic[2].Y + shipStatic[2].Height)))
            {
                if (usedShips[2] == 0)
                    return 0;
                else
                    return 3;
            }
            else if ((mouseDownPos.X > shipStatic[1].X) && (mouseDownPos.X < (shipStatic[1].X + shipStatic[1].Width)) 
                && (mouseDownPos.Y > shipStatic[1].Y) && (mouseDownPos.Y < (shipStatic[1].Y + shipStatic[1].Height)))
            {
                if (usedShips[1] == 0)
                    return 0;
                else
                    return 2;
            }
            else if ((mouseDownPos.X > shipStatic[0].X) && (mouseDownPos.X < (shipStatic[0].X + shipStatic[0].Width)) 
                && (mouseDownPos.Y > shipStatic[0].Y) && (mouseDownPos.Y < (shipStatic[0].Y + shipStatic[0].Height)))
            {
                if (usedShips[0] == 0)
                    return 0;
                else
                    return 1;
            }
            else
                return 0;
        }

        private void SetShipOnTile()
        {
            pickedTileX = (int)(Math.Ceiling((mousePos.X - userFieldR.X) / fieldTileSize));
            pickedTileY = (int)(Math.Ceiling((mousePos.Y - userFieldR.Y) / fieldTileSize));
            if (pickedTileX == 0) { pickedTileX = 1; }
            if (pickedTileY == 0) { pickedTileY = 1; }
            if (!(CheckPlacementAbility(pickedTileX, pickedTileY, draggingItem, isShipRotated))) { isTimerOn = true;  return; }
            if (isShipRotated)
            {
                fieldTiles[pickedTileX - 1, pickedTileY - 1] = 2;
                for (int i = 0; i <= draggingItem;  i++)
                    userMatrix[pickedTileX, pickedTileY + i] = 1;
            }
            else
            {
                fieldTiles[pickedTileX - 1, pickedTileY - 1] = 1;
                for (int i = 0; i <= draggingItem; i++)
                    userMatrix[pickedTileX + i, pickedTileY] = 1;
            }
            fieldTilesShips[pickedTileX-1, pickedTileY-1] = draggingItem;
            usedShips[draggingItem - 1] -= 1;
            usedShipsTotal++;
            if (usedShipsTotal == 10)
                startBattleSuggestion();
        }

        private void startBattleSuggestion()
        {

        }

        private void CheckLeftButtonReleased()
        {
            if (currentMouseState.LeftButton == ButtonState.Released)
            {
                if ((mousePos.X >= userFieldR.X) && (mousePos.X <= (userFieldR.X + userFieldR.Width)) && 
                    (mousePos.Y >= userFieldR.Y) && (mousePos.Y <= (userFieldR.Y + userFieldR.Height)))
                    if (draggingItem != 0)
                        SetShipOnTile();
                draggingItem = 0;
                isShipRotated = false;
            }
        }

        private void CheckLeftButtonReleasedOutboard()
        {

        }


        private void StartPage()
        {
            if (startGameCheck)
            {
                spriteBatch.Draw(userField, userFieldR, Color.White);
                spriteBatch.Draw(botField, botFieldR, Color.White);
                spriteBatch.Draw(lettersTexture, lettersTextureR_user, Color.White);
                spriteBatch.Draw(lettersTexture, lettersTextureR_bot, Color.White);
                spriteBatch.Draw(numbersTexture, numbersTextureR_user, Color.White);
                spriteBatch.Draw(numbersTexture, numbersTextureR_bot, Color.White);

                if (usedShips[3] != 0)
                    spriteBatch.Draw(ship[3], shipStatic[3], Color.White);
                else
                    spriteBatch.Draw(ship[3], shipStatic[3], Color.OrangeRed);
                if (usedShips[2] != 0)
                    spriteBatch.Draw(ship[2], shipStatic[2], Color.White);
                else
                    spriteBatch.Draw(ship[2], shipStatic[2], Color.OrangeRed);
                if (usedShips[1] != 0)
                    spriteBatch.Draw(ship[1], shipStatic[1], Color.White);
                else
                    spriteBatch.Draw(ship[1], shipStatic[1], Color.OrangeRed);
                if (usedShips[0] != 0)
                    spriteBatch.Draw(ship[0], shipStatic[0], Color.White);
                else
                    spriteBatch.Draw(ship[0], shipStatic[0], Color.OrangeRed);
            }
            if (draggingItem != 0)
            {
                if (isShipRotated)
                    spriteBatch.Draw(ship[draggingItem - 1], new Rectangle((int)(mousePos.X),
                    (int)(mousePos.Y), shipStatic[draggingItem - 1].Width, shipStatic[draggingItem - 1].Height), null,  Color.White,
                    MathHelper.PiOver2, new Vector2(ship[draggingItem - 1].Width / (draggingItem + 1),ship[draggingItem - 1].Height / 2), SpriteEffects.None, 0);
                else
                    spriteBatch.Draw(ship[draggingItem-1], new Rectangle((int)(mousePos.X - shipStatic[draggingItem-1].Width / (draggingItem + 1)), 
                    (int)(mousePos.Y - shipStatic[draggingItem - 1].Height / 2), shipStatic[draggingItem-1].Width, shipStatic[draggingItem-1].Height), Color.White);
            }
            if (isTimerOn)
            {
                spriteBatch.DrawString(defaultFont, "Ща маслину поймаешь", new Vector2(formWidth / 2, formHeight - 60), Color.Red);
            }

            for (int i = 0; i <= 9; i++) 
                for (int j = 0; j <= 9; j++)
                {
                    if (fieldTiles[i, j] == 1)
                    {
                        byte t = fieldTilesShips[i, j];
                        spriteBatch.Draw(ship[t - 1], new Rectangle(userFieldR.X + fieldTileSize * i + fieldTileSize / 10,
                            (userFieldR.Y + fieldTileSize * j + fieldTileSize / 10 + (int)(fieldTileSize * 0.5 - shipStatic[t-1].Height * 0.5)), shipStatic[t - 1].Width,
                            shipStatic[t - 1].Height), Color.White);
                    }
                    else if (fieldTiles[i,j] == 2)
                    {
                        byte t = fieldTilesShips[i, j];
                        spriteBatch.Draw(ship[t - 1], new Rectangle(userFieldR.X + fieldTileSize * i + fieldTileSize / 10 + shipStatic[t-1].Height,
                        (userFieldR.Y + fieldTileSize * j + fieldTileSize / 10 + (int)(fieldTileSize * 0.5 - shipStatic[t - 1].Height * 0.5)), shipStatic[t - 1].Width, shipStatic[t - 1].Height), null, Color.White,
                        MathHelper.PiOver2, new Vector2(0,0), SpriteEffects.None, 0);

                    }
                }
        }

        //для дебага. отображает в центре координаты курсора
        private void DebugString()
        {
            Vector2 fontOrigin = defaultFont.MeasureString(mousePosDebugString) / 2;
            spriteBatch.DrawString(defaultFont, mousePosDebugString, new Vector2(formWidth / 2, 20), Color.Black, 0, fontOrigin, 1.0f, SpriteEffects.None, 0.5f);
            spriteBatch.DrawString(defaultFont, draggingItem.ToString(), new Vector2(formWidth / 2, formHeight - 30), Color.Black);
        }
        private void PressEnterPage()
        {
            if (pressEnterCheck)
                spriteBatch.DrawString(defaultFont, "Нажмите", new Vector2((int)(formWidth * 0.3), formHeight / 2), Color.Black);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.WhiteSmoke);

            spriteBatch.Begin();
            StartPage();
            DebugString();
            PressEnterPage();

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}