﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace RemoteFridgeStorage
{
    /// <summary>
    /// Takes care of adding and removing elements for crafting.
    /// </summary>
    public class FridgeHandler
    {
        public HashSet<Chest> Chests { get; }
        private readonly NetObjectList<Chest> _netChests;

        /// <summary>
        /// Is true when the _categorizeChests is loaded so that the icon can be moved.
        /// </summary>
        private readonly bool _categorizeChestsLoaded;

        private readonly bool _cookingSkillLoaded;

        /// <summary>
        /// Texture button state included.
        /// </summary>
        private readonly ClickableTextureComponent _fridgeSelected;

        /// <summary>
        /// Texture button state excluded (default state)
        /// </summary>
        private readonly ClickableTextureComponent _fridgeDeselected;

        /// <summary>
        /// If the chest is currently open.
        /// </summary>
        private bool _opened;

        /// <summary>
        /// Creates a new handler for fridge items.
        /// </summary>
        /// <param name="textureFridge"></param>
        /// <param name="textureFridge2"></param>
        /// <param name="categorizeChestsLoaded"></param>
        public FridgeHandler(Texture2D textureFridge, Texture2D textureFridge2, bool categorizeChestsLoaded)
        {
            _categorizeChestsLoaded = categorizeChestsLoaded;
            _opened = false;
            _fridgeSelected = new ClickableTextureComponent(Rectangle.Empty, textureFridge, Rectangle.Empty, 1f);
            _fridgeDeselected = new ClickableTextureComponent(Rectangle.Empty, textureFridge2, Rectangle.Empty, 1f);
            Chests = new HashSet<Chest>();
            _netChests = new NetObjectList<Chest>();
        }

        /// <summary>
        /// Update the position of the button.
        /// </summary>
        private void UpdatePos()
        {
            var menu = Game1.activeClickableMenu;
            if (menu == null) return;

            var xOffset = 0.0;
            var yOffset = 1.0;
            if (_categorizeChestsLoaded)
            {
                xOffset = -1.0;
                yOffset = -0.25;
            }

            var xScaledOffset = (int) (xOffset * Game1.tileSize);
            var yScaledOffset = (int) (yOffset * Game1.tileSize);

            _fridgeSelected.bounds = _fridgeDeselected.bounds = new Rectangle(
                menu.xPositionOnScreen - 17 * Game1.pixelZoom + xScaledOffset,
                menu.yPositionOnScreen + yScaledOffset + Game1.pixelZoom * 5, 16 * Game1.pixelZoom,
                16 * Game1.pixelZoom);
        }

        /// <summary>
        /// Handle the click event if it was on the fridge icon.
        /// </summary>
        /// <param name="eventArgsInput"></param>
        public void HandleClick(EventArgsInput eventArgsInput)
        {
            var chest = GetOpenChest();
            if (chest == null) return;

            var screenPixels = eventArgsInput.Cursor.ScreenPixels;

            if (!_fridgeSelected.containsPoint((int) screenPixels.X, (int) screenPixels.Y)) return;

            Game1.playSound("smallSelect");

            if (Chests.Contains(chest))
            {
                Chests.Remove(chest);
            }
            else
            {
                Chests.Add(chest);
            }
        }

        /// <summary>
        /// Gets the chest that is currently open or null if no chest is open.
        /// </summary>
        /// <returns>The chest that is open</returns>
        private static Chest GetOpenChest()
        {
            if (Game1.activeClickableMenu == null)
                return null;

            if (!(Game1.activeClickableMenu is ItemGrabMenu)) return null;

            var menu = (ItemGrabMenu) Game1.activeClickableMenu;
            if (menu.behaviorOnItemGrab?.Target is Chest chest)
                return chest;

            return null;
        }

        /// <summary>
        /// Draw the fridge button
        /// </summary>
        public void DrawFridge()
        {
            var openChest = GetOpenChest();
            if (openChest == null) return;

            var farmHouse = Game1.getLocationFromName("farmHouse") as FarmHouse;

            if (openChest == farmHouse?.fridge.Value || Game1.activeClickableMenu == null ||
                !openChest.playerChest.Value) return;

            UpdatePos();
            if (Chests.Contains(openChest))
                _fridgeSelected.draw(Game1.spriteBatch);
            else
                _fridgeDeselected.draw(Game1.spriteBatch);

            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero,
                4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Load all fridges.
        /// </summary>
        public void AfterLoad()
        {
            //
            Chests.Clear();
            var farmHouse = Game1.getLocationFromName("farmHouse") as FarmHouse;
            foreach (var gameLocation in Game1.locations)
            {
                foreach (var gameLocationObject in gameLocation.objects.Values)
                {
                    if (!(gameLocationObject is Chest chest)) continue;

                    if (chest.fridge.Value && chest != farmHouse?.fridge.Value)
                    {
                        Chests.Add(chest);
                        chest.fridge.Value = false;
                    }
                }
            }
        }

        /// <summary>
        /// Hacky way to store which chests are selected
        /// </summary>
        public void BeforeSave()
        {
            foreach (var chest in Chests)
            {
                chest.fridge.Value = true;
            }
        }

        /// <summary>
        /// Reset the fridge booleans
        /// </summary>
        public void AfterSave()
        {
            //Reset the fridge flag for all chests.
            foreach (var chest in Chests)
            {
                chest.fridge.Value = false;
            }
        }

        /// <summary>
        /// Listen to ticks update to determine when a chest opens or closes.
        /// </summary>
        public void Game_Update()
        {
            var chest = GetOpenChest();
            if (chest == null && _opened)
            {
                //Chest close event;
            }

            if (chest != null && !_opened)
            {
                //Chest open event
                UpdatePos();
            }

            _opened = chest != null;
        }

        /// <summary>
        /// Replace the menu 
        /// </summary>
        /// <param name="argEvents"></param>
        public void LoadMenu(EventArgsClickableMenuChanged argEvents)
        {
            if (!_categorizeChestsLoaded || ModEntry.Instance.CookinSkillApi == null)
            {
                Game1.activeClickableMenu = new RemoteFridgeCraftingPage(argEvents.NewMenu, this);
            }
        }
    }
}