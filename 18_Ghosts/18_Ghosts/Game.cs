﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace _18_Ghosts
{
    class Game
    {
        private Random rand;

        private Renderer render;

        private Tile[,] board;
        private List<Ghost> dungeon;
        private List<ConsoleColor> availableColor;

        private Player currentPlayer;
        private Player playerOne;
        private Player playerTwo;

        private int winCondition;

        private bool chooseFreeGhost;
        private bool validChoice;

        // Posição x e y actual
        private int xPos;
        private int yPos;



        public Game(Renderer render)
        {
            this.render = render;
            
            board = new Tile[5, 5];

            dungeon = new List<Ghost>();

            availableColor = new List<ConsoleColor>();

            rand = new Random();
        }

        public void Init()
        {
            // Inicializar a board 
            board[0, 0] = new Tile("╔", ConsoleColor.Blue);
            board[0, 1] = new Tile("╦", ConsoleColor.Red);
            board[0, 2] = new Tile(ConsoleColor.Red, TileOrientation.Up);
            board[0, 3] = new Tile("╦", ConsoleColor.Blue);
            board[0, 4] = new Tile("╗", ConsoleColor.Red);

            board[1, 0] = new Tile("╠", ConsoleColor.Yellow);
            board[1, 1] = new Tile("A", ConsoleColor.Black, true);
            board[1, 2] = new Tile("╬", ConsoleColor.Yellow);
            board[1, 3] = new Tile("A", ConsoleColor.Black, true);
            board[1, 4] = new Tile("╣", ConsoleColor.Yellow);
                                     
            board[2, 0] = new Tile("╠", ConsoleColor.Red);
            board[2, 1] = new Tile("╬", ConsoleColor.Blue);
            board[2, 2] = new Tile("╬", ConsoleColor.Red);
            board[2, 3] = new Tile("╬", ConsoleColor.Blue);
            board[2, 4] = new Tile(ConsoleColor.Yellow, TileOrientation.Right);

            board[3, 0] = new Tile("╠", ConsoleColor.Blue);
            board[3, 1] = new Tile("A", ConsoleColor.Black, true);
            board[3, 2] = new Tile("╬", ConsoleColor.Yellow);
            board[3, 3] = new Tile("A", ConsoleColor.Black, true);
            board[3, 4] = new Tile("╣", ConsoleColor.Red);
                                   
            board[4, 0] = new Tile("╚", ConsoleColor.Yellow);
            board[4, 1] = new Tile("╩", ConsoleColor.Red);
            board[4, 2] = new Tile(ConsoleColor.Blue, TileOrientation.Down);
            board[4, 3] = new Tile("╩", ConsoleColor.Blue);
            board[4, 4] = new Tile("╝", ConsoleColor.Yellow);
            
            playerOne = new Player(PlayerType.A);
            playerTwo = new Player(PlayerType.B);

            currentPlayer = playerOne;

            chooseFreeGhost = false;
            PlaceGhosts();

            GameLoop();
        }

        private void GameLoop()
        {
            do
            {
                // Dar render ao tabuleiro
                render.RenderBoard(board);

                // Quando o jogador tem fantasmas para jogar é obrigado a faze-lo
                if (currentPlayer.Ghosts.Count != 0)
                {
                    validChoice = false;

                    do
                    {
                        render.PlaceGhost(currentPlayer.Ghosts[0].GhostColor);

                        xPos = UpdateXY();

                        render.PlaceGhostY();

                        yPos = UpdateXY();

                        if (board[yPos, xPos].TileGhost == null && board[yPos, xPos].TileColor == currentPlayer.Ghosts[0].GhostColor)
                        {
                            validChoice = true;
                        }
                        else
                        {
                            render.ErrorMessage();
                        }

                    } while (!validChoice);

                    board[yPos, xPos].TileGhost = currentPlayer.Ghosts[0];
                    currentPlayer.Ghosts.RemoveAt(0);
                }
                else
                {
                    string choice;

                    if (dungeon.Count != 0 && CheckFreeableGhost())
                    {
                        do
                        {
                            render.ChoosePlay();

                            choice = Console.ReadLine();
                            choice = choice.ToLower();

                            if (choice != "f" && choice != "m")
                            {
                                render.ErrorMessage();
                            }
                        } while (choice != "f" && choice != "m");

                        chooseFreeGhost = choice == "f";
                    }

                    if (dungeon.Count == 0 || !chooseFreeGhost)
                    {
                        do
                        {
                            // Pede ao utilizador a localização do fantasma que ele quer mover
                            render.GhostToMove('X');

                            // Da update à posição selecionada pelo jogador
                            xPos = UpdateXY();

                            // Pede ao utilizador a localização em Y do fantasma que ele quer mexer
                            render.GhostToMove('Y');

                            // Da update à posição selecionada pelo jogador
                            yPos = UpdateXY();

                        } while (!CheckHouse());

                        MoveGhost();
                    }
                    else if (chooseFreeGhost)
                    {
                        chooseFreeGhost = false;
                        validChoice = false;

                        do
                        {
                            render.ChooseColorToFree(availableColor);

                            choice = Console.ReadLine();
                            choice = choice.ToLower();

                            switch (choice)
                            {
                                case "r":
                                    if (availableColor.Contains(ConsoleColor.Red))
                                    {
                                        FreeGhostFromDungeon(ConsoleColor.Red);
                                        validChoice = true;
                                    }
                                    break;
                                case "b":
                                    if (availableColor.Contains(ConsoleColor.Blue))
                                    {
                                        FreeGhostFromDungeon(ConsoleColor.Blue);
                                        validChoice = true;
                                    }
                                    break;
                                case "y":
                                    if (availableColor.Contains(ConsoleColor.Yellow))
                                    {
                                        FreeGhostFromDungeon(ConsoleColor.Yellow);
                                        validChoice = true;
                                    }
                                    break;
                                default:
                                    render.ErrorMessage();
                                    break;
                            }

                        } while (!validChoice);

                        Console.ReadKey(true);
                    }
                }

                CheckPortalRotation();

                if (playerOne.EscapedGhosts >= 3)
                {
                    winCondition = 1;
                }
                if (playerTwo.EscapedGhosts >= 3)
                {
                    winCondition = winCondition == 1 ? 3 : 2;
                }

                currentPlayer = currentPlayer == playerOne ? playerTwo : playerOne;

            } while (winCondition == 0);

            render.RenderWinner(winCondition);
            Console.ReadKey();
        }

        private void FreeGhostFromDungeon(ConsoleColor color)
        {
            for (int i = 0; i < dungeon.Count; i++)
            {
                if (dungeon[i].GhostColor == color && dungeon[i].Owner == currentPlayer.Type)
                {
                    if (currentPlayer.Type == PlayerType.A)
                    {
                        playerTwo.Ghosts.Add(dungeon[i]);
                        dungeon.RemoveAt(i);
                        render.GhostWasFreed(color);
                        break;
                    }
                    else
                    {
                        playerOne.Ghosts.Add(dungeon[i]);
                        dungeon.RemoveAt(i);
                        render.GhostWasFreed(color);
                        break;
                    }
                }
            }
        }

        private bool CheckFreeableGhost()
        {
            bool freeable = false;

            availableColor.Clear();

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (board[y, x].TileGhost == null)
                    {
                        for (int i = 0; i < dungeon.Count; i++)
                        {
                            if (dungeon[i].Owner == currentPlayer.Type &&
                                dungeon[i].GhostColor == board[y, x].TileColor)
                            {
                                if (availableColor.Count == 0 || !availableColor.Contains(dungeon[i].GhostColor))
                                {
                                    availableColor.Add(dungeon[i].GhostColor);
                                }
                                freeable = true;
                            }
                        }
                    }
                }
            }

            return freeable;
        }

        // Verificamos se o fantasma pode escapar
        private void GhostTryEscape(int x, int y, ConsoleColor color)
        {
            if (board[y, x].TileGhost != null && board[y, x].TileGhost.GhostColor == color)
            {
                if (board[y, x].TileGhost.Owner == PlayerType.A)
                {
                    playerOne.EscapedGhosts++;
                }
                else
                {
                    playerTwo.EscapedGhosts++;
                }

                board[y, x].TileGhost = null;
            }
        }

        private void CheckPortalRotation()
        {
            // Verificar ao pe do portal vermelho
            switch (board[0, 2].Orientation)
            {
                case TileOrientation.Left:
                    GhostTryEscape(1, 0, ConsoleColor.Red);
                    break;
                case TileOrientation.Down:
                    GhostTryEscape(2, 1, ConsoleColor.Red);
                    break;
                case TileOrientation.Right:
                    GhostTryEscape(3, 0, ConsoleColor.Red);
                    break;
            }
            // Verificar ao pe do portal amarelo
            switch (board[2, 4].Orientation)
            {
                case TileOrientation.Up:
                    GhostTryEscape(4, 1, ConsoleColor.Yellow);
                    break;
                case TileOrientation.Left:
                    GhostTryEscape(3, 2, ConsoleColor.Yellow);
                    break;
                case TileOrientation.Down:
                    GhostTryEscape(4, 3, ConsoleColor.Yellow);
                    break;
            }
            // Verificar ao pe do portal azul
            switch (board[4, 2].Orientation)
            {
                case TileOrientation.Left:
                    GhostTryEscape(1, 4, ConsoleColor.Blue);
                    break;
                case TileOrientation.Up:
                    GhostTryEscape(2, 3, ConsoleColor.Blue);
                    break;
                case TileOrientation.Right:
                    GhostTryEscape(3, 4, ConsoleColor.Blue);
                    break;
            }
        }

        private int UpdateXY()
        {
            int keyInt;

            // Converte para string e remove todos os characteres que não estejam entre 0 e 4.
            while (!int.TryParse(Regex.Replace(Console.ReadKey().Key.ToString(), "[^0-4]", ""), out keyInt))
            {
                // Enquanto o input for incorreto mostrar uma mensagem de erro.
                render.ErrorMessage();
            }

            return keyInt;
        }

        private void MoveGhost()
        {
            int x;
            int y;
            ConsoleKey key;

            render.MoveGhost();

            do
            {
                x = xPos;
                y = yPos;

                while (!(key = Console.ReadKey(true).Key).ToString().Contains("Arrow"))
                {
                    render.ErrorMessage();
                }

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                            y--;
                        break;
                    case ConsoleKey.DownArrow:
                            y++;
                        break;
                    case ConsoleKey.RightArrow:
                            x++;
                        break;
                    case ConsoleKey.LeftArrow:
                            x--;
                        break;
                }

                if (x > 4 || x < 0 || y > 4 || y < 0 || board[y, x].isExitTile || 
                    (board[y, x].TileGhost != null && 
                    board[y, x].TileGhost.GhostColor == board[yPos, xPos].TileGhost.GhostColor))
                {
                    render.ErrorMessage();
                }

            } while (x > 4 || x < 0 || y > 4 || y < 0 || board[y, x].isExitTile ||
                    (board[y, x].TileGhost != null && 
                    board[y, x].TileGhost.GhostColor == board[yPos, xPos].TileGhost.GhostColor));
            
            if (board[y, x].TileGhost != null)
            {
                ConsoleColor myColor = board[yPos, xPos].TileGhost.GhostColor;
                ConsoleColor otherColor = board[y, x].TileGhost.GhostColor;

                switch (myColor)
                {
                    case ConsoleColor.Red:
                        if (otherColor == ConsoleColor.Yellow)
                        {
                            dungeon.Add(board[yPos, xPos].TileGhost);
                            board[0, 2].Orientation++;
                        }
                        else
                        {
                            dungeon.Add(board[y, x].TileGhost);
                            board[y, x].TileGhost = board[yPos, xPos].TileGhost;
                            board[4, 2].Orientation++;
                        }
                        break;
                    case ConsoleColor.Blue:
                        if (otherColor == ConsoleColor.Red)
                        {
                            dungeon.Add(board[yPos, xPos].TileGhost);
                            board[4, 2].Orientation++;
                        }
                        else
                        {
                            dungeon.Add(board[y, x].TileGhost);
                            board[y, x].TileGhost = board[yPos, xPos].TileGhost;
                            board[2, 4].Orientation++;
                        }
                        break;
                    case ConsoleColor.Yellow:
                        if (otherColor == ConsoleColor.Blue)
                        {
                            dungeon.Add(board[yPos, xPos].TileGhost);
                            board[2, 4].Orientation++;
                        }
                        else
                        {
                            dungeon.Add(board[y, x].TileGhost);
                            board[y, x].TileGhost = board[yPos, xPos].TileGhost;
                            board[0, 2].Orientation++;
                        }
                        break;
                }
            }
            else if (board[y, x].isMirrorTile)
            {
                x += 2;
                y += 2;
                x = x == 5 ? 1 : x;
                y = y == 5 ? 1 : y;

                board[y, x].TileGhost = board[yPos, xPos].TileGhost;
            }
            else
            {
                board[y, x].TileGhost = board[yPos, xPos].TileGhost;
            }

            board[yPos, xPos].TileGhost = null;
        }

        // Verifica se a casa selecionada tem um fantasma do jogador
        private bool CheckHouse() => 
            board[yPos, xPos].TileGhost != null && board[yPos, xPos].TileGhost.Owner == currentPlayer.Type;

        private void PlaceGhosts()
        {
            Player currentPlaying;

            bool ghostPlaced;

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    ghostPlaced = false;

                    if (board[y, x].isExitTile || board[y, x].isMirrorTile)
                    {
                        continue;
                    }

                    currentPlaying = rand.NextDouble() < 0.5 ? playerOne : playerTwo;

                    ghostPlaced = TryPlacing(currentPlaying, x, y);

                    if (!ghostPlaced)
                    {
                        currentPlaying = currentPlaying == playerOne ? playerTwo : playerOne;

                        TryPlacing(currentPlaying, x, y);
                    }
                }
            }
        }

        private bool TryPlacing(Player currentPlaying, int x, int y)
        {
            foreach (Ghost ghost in currentPlaying.Ghosts)
            {
                if (ghost.GhostColor == board[y, x].TileColor && !ghost.InGame)
                {
                    ghost.InGame = true;
                    board[y, x].TileGhost = ghost;
                    currentPlaying.Ghosts.Remove(ghost);
                    return true;
                }
            }
            return false;
        }

    }
}
