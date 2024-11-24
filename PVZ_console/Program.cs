using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Transactions;
namespace PVZ_console
{
    internal class Program
    {
        //global variables | Organized as best as my little brain can

        //Entity lists
        static string notesFilePath = @"..\..\..\Entities.txt";
        static string zombGeneration = @"..\..\..\ZombieGeneration.txt";
        static List<PlantBrain> plants = new List<PlantBrain>();
        static List<ZombieBrain> zombies = new List<ZombieBrain>();
        static List<Projectile> projectiles = new List<Projectile>();

        //Cell type lists
        static List<Cell> nonInteract = new List<Cell>();
        static List<Cell> landSlot = new List<Cell>();
        static List<Cell> seedSlot = new List<Cell>();

        //In-game storage items
        static object mouseQueue = null; //variable to store what the mouse currently is storing (either nothing or a plant to place)
        static List<Cell> cell_list = new List<Cell>(); //list to store every cells' info
        static List<ZombieGenerationInfo> zombGen = new List<ZombieGenerationInfo>(); //Temporary list to store zombie generation
        static Dictionary<Cell, double> newCellTimers = new Dictionary<Cell, double>();
        static int sunQTY = 0; //Player sun qty

        //Game state checks
        static string[] gameStates = { "Menu", "In-game", "Paused" };
        static string curState = gameStates[0];
        static object lastMovedProj = null;

        //Conditional checks for loading
        static bool gameModeDetect = false;
        static bool generatedCells = false;
        static bool preload = false;

        //In-Game timer for storing game process time
        static double gameTimer;
        //==================================================================================================================//

        //External stoopid code to import to make some important window related and mouse related functions work

        // We need to use unmanaged code
        [DllImport("user32.dll")]

        // GetCursorPos() makes everything possible
        static extern bool GetCursorPos(ref Point lpPoint);

        //More unmanaged code
        [DllImport("user32.dll")]
        //Call window function
        static extern IntPtr GetForegroundWindow();

        //MORE unmanaged code
        [DllImport("user32.dll")]
        //Get window sizes
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        //Create a struct to store the coord system for out window
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        //Run necesarry functions every frame
        static void Main(string[] args)
        {
            ScreenSize();

            //Check if data has yet to be preloaded --> wont reload everything every frame the game is run
            if (!preload)
            {
                //Always create entity list on execute
                initEntities();
            }
            else
            {
                Draw();
            }
            //Sleep for 1/4 of a second --> 4 fps? doesnt flash eyes too bad
            Thread.Sleep(250);

            if (curState == "In-game")
                gameTimer += 0.25;
            //Clear console (failsafe) and loop :D
            Console.Clear();
            Main(null);
        }

        //Force screen size to display everything adequatly
        static void ScreenSize()
        {
            int desiredH = 30;
            int desiredW = 60;

            bool FixedWindow = (Console.WindowHeight != desiredH) && (Console.WindowWidth != desiredW);
            while (FixedWindow)
            {
                Console.WriteLine($"The desired screen size is {desiredW} by {desiredH}.");
                Console.WriteLine($"Currently, the screen size is {Console.WindowWidth} by {Console.WindowHeight}");
                Console.WriteLine("Make adjustments to the window, then press any key to check again");
                Console.WriteLine("WARNING!!! DO NOT ADJUST TEXT SIZE, OTHERWISE CELL SPACING WILL BREAK! YOU WILL NOT BE ABLE TO PLAY THE GAME!");
                Console.ReadKey(true);
                Console.Clear();
                FixedWindow = (Console.WindowHeight != desiredH) && (Console.WindowWidth != desiredW);
            }
        }

        //Generate list of all entities ; This is usually only a ONE-TIME process, if everything goes well
        static void initEntities()
        {
            //Create list of all plants and zombies in game


            //Using StreamRead, open all entities file path
            using (var sr = new StreamReader(notesFilePath))
            {
                while (sr.Peek() >= 0)
                {
                    //Check whether each line is for zombies or plants
                    var line = sr.ReadLine();
                    if (line.StartsWith("p_"))
                    {
                        //Break string into pieces --> store the info in an array and create a plant from given data
                        string[] subStrings = line.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
                        plants.Add(new PlantBrain(subStrings[0], subStrings[1], Convert.ToInt32(subStrings[2]), Convert.ToInt32(subStrings[3]), subStrings[4], subStrings[5]));
                    }
                    else if (line.StartsWith("z_"))
                    {
                        //Break string into pieces --> store the info in an array and create a zombie from given data
                        string[] subStrings = line.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
                        zombies.Add(new ZombieBrain(subStrings[0], subStrings[1], subStrings[2], subStrings[3], Convert.ToDouble(subStrings[4])));
                    }
                    else if (line.StartsWith("s_"))
                    {
                        //Break string into pieces --> store the info in an array and create a zombie from given data
                        string[] subStrings = line.Split("|", StringSplitOptions.RemoveEmptyEntries);
                        projectiles.Add(new Projectile(subStrings[0], Convert.ToInt32(subStrings[1]), subStrings[2], subStrings[3]));
                    }
                }
            }

            using(var sr = new StreamReader(zombGeneration))
            {
                while(sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    if (line.StartsWith("zgen_"))
                    {
                        string[] substrings = line.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
                        ZombieBrain zombToAdd = null;
                        foreach (ZombieBrain zomb in zombies)
                        {
                            if (zomb.Zombie_Name.Trim().Normalize() == substrings[2].Trim().Normalize())
                            {
                                zombToAdd = zomb;
                            }
                        }
                        zombGen.Add(new ZombieGenerationInfo(Convert.ToInt16(substrings[1]), zombToAdd, substrings[3], Convert.ToDouble(substrings[4]), Convert.ToBoolean(substrings[5]), Convert.ToInt16(substrings[6])));
                    }
                }
            }

            preload = true;
        }

        //Draw game screen
        static void Draw()
        {
            //Clear previous frame
            Console.Clear();
            //Check our current state --> outdated method imo but works for now ig
            switch (curState)
            {
                case "Menu":
                    DMM();
                    break;
                case "In-game":
                    DIG();
                    GameLogic();
                    lastMovedProj = null;
                    break;
                case "Paused":
                    Console.WriteLine("Game paused");
                    break;
            }
            //End of frame--> accept player inputs
            PlayerInputs();
        }

        //Draw main menu
        static void DMM()
        {
            Console.WriteLine("Main menu");
            Console.WriteLine("1.Options\n2.Play game\n3.Exit\n4.Credits");
        }

        //Draw in-game
        static void DIG()
        {
            //Vars in this section are STATIC --> aren't expected to change!
            int numOfRows = 10;
            int numOfCols = 7;

            if (!generatedCells)
            {
                InitCells(numOfCols, numOfRows);
                generatedCells = true;
            }

            //Cell design --> needs to be under init cell so that you can store cell mid contents
            string cellTop = "╔═╗";
            string cellBot = "╚═╝";

            char cellRow = 'A';

            for (int i = 0; i < numOfCols; i++)
            {
                for (int j = 0; j < numOfRows; j++)
                {
                    Console.Write($"{cellTop}");
                }
                Console.Write("\n");
                for (int j = 0; j < numOfRows; j++)
                {
                    string curId = $"{cellRow}{j + 1}";

                    //Always print sunQTY for the first cell ; Probably a better way to do this but it works for now
                    if (curId == "A1")
                    {
                        Console.ResetColor();
                        Console.Write("║");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write(sunQTY);
                        Console.ResetColor();
                        Console.Write("║");
                    }
                    else
                    {
                        object displayedChar = (cell_list.Find(Cell => Cell.cell_ID == curId).cell_Contents.LastOrDefault());

                        foreach (PlantBrain plant in plants)
                        {
                            if (displayedChar == plant)
                            {
                                Console.ResetColor();
                                Console.Write("║");
                                Console.ForegroundColor = SetConsoleColor(plant.color);
                                Console.Write(plant.symbol);
                                Console.ResetColor();
                                Console.Write("║");

                                break;
                            }
                        }

                        foreach (ZombieBrain zomb in zombies)
                        {
                            if (displayedChar == zomb)
                            {
                                Console.ResetColor();
                                Console.Write("║");
                                Console.ForegroundColor = SetConsoleColor(zomb.color);
                                Console.Write(zomb.symbol);
                                Console.ResetColor();
                                Console.Write("║");

                                break;
                            }
                        }

                        foreach (Projectile proj in projectiles)
                        {
                            if (displayedChar == proj)
                            {
                                Console.ResetColor();
                                Console.Write("║");
                                Console.ForegroundColor= SetConsoleColor(proj.color);
                                Console.Write(proj.symbol);
                                Console.ResetColor();
                                Console.Write("║");

                                break;
                            }
                        }

                        if (displayedChar == "!")
                        {
                            Console.ResetColor();
                            Console.Write("║");
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write("!");
                            Console.ResetColor();
                            Console.Write("║");
                        }
                        if (displayedChar == " ")
                        {
                            Console.Write($"║{displayedChar}║");
                        }
                        if(displayedChar == "-")
                        {
                            Console.Write($"║{displayedChar}║");
                        }
                    }
                }
                Console.Write("\n");
                for (int j = 0; j < numOfRows; j++)
                {
                    Console.Write($"{cellBot}");
                }
                cellRow++;
                Console.Write("\n");
            }
        }

        static ConsoleColor SetConsoleColor(string color)
        {
            switch (color)
            {
                case "Black":
                    return ConsoleColor.Black;
                case "DarkBlue":
                    return ConsoleColor.DarkBlue;
                case "DarkGreen":
                    return ConsoleColor.DarkGreen;
                case "DarkCyan":
                    return ConsoleColor.DarkCyan;
                case "DarkRed":
                    return ConsoleColor.DarkRed;
                case "DarkMagenta":
                    return ConsoleColor.DarkMagenta;
                case "DarkYellow":
                    return ConsoleColor.DarkYellow;
                case "Gray":
                    return ConsoleColor.Gray;
                case "DarkGray":
                    return ConsoleColor.DarkGray;
                case "Blue":
                    return ConsoleColor.Blue;
                case "Green":
                    return ConsoleColor.Green;
                case "Cyan":
                    return ConsoleColor.Cyan;
                case "Red":
                    return ConsoleColor.Red;
                case "Magenta":
                    return ConsoleColor.Magenta;
                case "Yellow":
                    return ConsoleColor.Yellow;
                case "White":
                    return ConsoleColor.White;
                default:
                    return ConsoleColor.Red;
            }
        }

        //Create cell references ; ONLY DO THIS ONCE AT THE BEGINNING OF EVERY GAME RUN
        static void InitCells(int Cols, int Rows)
        {
            //Create vars for cell top left and bottom right (somehow idk man)
            (int, int) cellTopLeft = (1, 1);
            (int, int) cellBotRight = (3, 3);
            //Cell_ID tags
            char cellRow = 'A';

            //First : generate all cells
            for (int i = 0; i < Cols; i++)
            {
                for (int j = 0; j < Rows; j++)
                {
                    List<object> cellObjs = new List<object>();

                    cellObjs.Add(" ");
                    cell_list.Add(new Cell($"{cellRow}{j + 1}", cellTopLeft, cellBotRight, cellObjs));

                    cellTopLeft.Item1 += 4;
                    cellBotRight.Item1 += 4;
                }
                cellRow++;
                cellTopLeft.Item2 += 3;
                cellBotRight.Item2 += 3;
                cellTopLeft.Item1 = 1;
                cellBotRight.Item1 = 3;
            }

            //Second : Organize cells in sections (plot, non-interact, seeds)
            nonInteract.Add(cell_list[0]);
            for (int i = 10; i < 20; i++)
            {
                nonInteract.Add(cell_list[i]);
            }

            for (int i = 1; i < 10; i++)
            {
                seedSlot.Add(cell_list[i]);
            }

            for (int i = 0; i < plants.Count; i++)
            {
                seedSlot[i].cell_Contents.Add(plants[i]);
            }

            foreach (Cell pass in nonInteract)
            {
                if (pass.cell_Contents.Contains(sunQTY))
                {
                    continue;
                }
                else
                {
                    pass.cell_Contents.Add("-");
                }
            }

            for (int i = 20; i < cell_list.Count(); i++)
            {
                landSlot.Add(cell_list[i]);
            }
        }

        //Function to get mousePosition. Returns (int, int) --> relative mousePOS to console
        static (int, int) GetMouseInput()
        {
            // New point that will be updated by the function with the current coordinates
            Point defPnt = new Point();

            // Call the function and pass the Point, defPnt
            GetCursorPos(ref defPnt);

            //Get our consoleWindow position for reference
            IntPtr consoleHandle = GetForegroundWindow();

            //Get our window size with coords
            GetWindowRect(consoleHandle, out RECT consoleRect);

            //Return mouse position for future ref
            return (defPnt.X - consoleRect.Left, defPnt.Y - consoleRect.Top);
        }

        //Function to check if mouse is within window --> returns bool
        static bool WithinWindow()
        {
            //Get our consoleWindow position for reference
            IntPtr consoleHandle = GetForegroundWindow();

            //Get our window size with coords
            GetWindowRect(consoleHandle, out RECT consoleRect);

            // New point that will be updated by the function with the current coordinates
            Point defPnt = new Point();

            // Call the function and pass the Point, defPnt
            GetCursorPos(ref defPnt);

            bool xInRange = defPnt.X >= consoleRect.Left && defPnt.X <= consoleRect.Right;
            bool yInRange = defPnt.Y >= consoleRect.Top && defPnt.Y <= consoleRect.Bottom;
            return xInRange && yInRange;
        }

        //Getting player mouse inputs and accept command input --> will be used to detect 'Grab plant' / 'Place plant' / 'Collect sun'
        static void PlayerInputs()
        {
            //Start by getting mousePOS every 'frame'
            (int, int) MousePos = GetMouseInput();
            bool isInWindow = WithinWindow();
            (double, double) convertedCharLength = CharToWindow();

            switch (curState)
            {
                case "In-game":
                    //Check for input WITHOUT blocking script. W/o console.KeyAvailable, the rest of the code would hang :\
                    // Loop this 10 times --> ensures proper input capture
                    for (int k = 0; k < 10; k++)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Spacebar && isInWindow)
                        {
                            for (int i = 0; i < cell_list.Count; i++)
                            {
                                if (IsInCell(convertedCharLength, cell_list[i], MousePos))
                                {
                                    DoCellAction(cell_list[i]);
                                }
                            }
                        }
                    }

                    break;

                case "Menu":

                    if (curState == gameStates[0] && !gameModeDetect)
                    {
                        for (int k = 0; k < 10; k++)
                        {
                            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.A && !gameModeDetect)
                            {
                                gameModeDetect = true;
                                curState = gameStates[1];
                                break;
                            }
                        }
                    }

                    break;

                case "Pause":
                    break;
            }

            if (!isInWindow)
            {
                Console.WriteLine("Outside game area! Return back to keep playing!");
            }
        }

        //Convert length of characters in console to global window coords
        static (double, double) CharToWindow()
        {
            //Get our consoleWindow position for reference
            IntPtr consoleHandle = GetForegroundWindow();

            //Get our window size with coords
            GetWindowRect(consoleHandle, out RECT consoleRect);

            double hori = consoleRect.Right - consoleRect.Left;
            double vert = consoleRect.Bottom - consoleRect.Top;

            double convertedLengthHori = hori / Console.WindowWidth;
            double convertedLengthVert = vert / Console.WindowHeight;

            return (convertedLengthHori, convertedLengthVert);
        }

        //Check to see if mouse is clicking in a specific cell --> returns bool depending on if mouse is in cell
        static bool IsInCell((double, double) conv, Cell cellToCheck, (int, int) mousePOS)
        {
            double cellRow = Double.Parse(Regex.Matches(cellToCheck.cell_ID, @"\d+").Cast<Match>().Last().Value);
            double cellRow_Adjusted = Math.Clamp(cellRow - 2, 0, 1000);
            (double, double) leftCorner = ((cellToCheck.cornerL.Item1 * conv.Item1) - (13.25 * cellRow_Adjusted), (cellToCheck.cornerL.Item2 * conv.Item2) + (0.85 * 13));
            (double, double) rightCorner = ((cellToCheck.cornerR.Item1 * conv.Item1) - (13.25 * cellRow_Adjusted), (cellToCheck.cornerR.Item2 * conv.Item2) + (0.85 * 13));

            bool isInXRange = (mousePOS.Item1 > leftCorner.Item1) && (mousePOS.Item1 < rightCorner.Item1);
            bool isInYRange = (mousePOS.Item2 > leftCorner.Item2) && (mousePOS.Item2 < rightCorner.Item2);
            return (isInXRange && isInYRange);
        }

        //Check the cell we pressed --> do actions related to A. current cell condition | B. current mouse condition
        static void DoCellAction(Cell cellChecked)
        {
            if (cellChecked.cell_Contents.Contains("!")) //temp fix for now
            {
                sunQTY++;
                sunQTY = Math.Clamp(sunQTY, 0, 9);
                cellChecked.cell_Contents.Remove("!");
                return;
            }

            if (seedSlot.Contains(cellChecked))
            {
                if (mouseQueue == null)
                {
                    PlantBrain plantInSeed = null;
                    foreach (PlantBrain plant in plants)
                    {
                        if (plant == cellChecked.cell_Contents.Last())
                        {
                            plantInSeed = plant;
                            break;
                        }
                    }

                    if (sunQTY >= plantInSeed.Sun_cost)
                    {
                        mouseQueue = plantInSeed;
                        sunQTY = sunQTY - plantInSeed.Sun_cost;
                    }
                }
            }
            else if (landSlot.Contains(cellChecked))
            {
                if (mouseQueue != null && cellChecked.cell_Contents.Count == 1)
                {
                    cellChecked.cell_Contents.Add(mouseQueue);
                    mouseQueue = null;

                    newCellTimers.Add(cellChecked, gameTimer);
                    return;
                }
            }
            return;
        }

        static void GameLogic()
        {
            //genSunTime dictates how many seconds before spawning sun
            //Divide global timer by this amount to dictate when to spawn sun
            Random rnd = new Random();
            int placesun = rnd.Next(0, cell_list.Count());
            int genSunTime = 8;

            //Check if the sun can be generated, and if that cell is a landSlot
            if (gameTimer % genSunTime == 0 && landSlot.Contains(cell_list[placesun]))
            {
                //Make sure the cell doesnt already have sun!
                if (!cell_list[placesun].cell_Contents.Contains("!"))
                {
                    cell_list[placesun].cell_Contents.Add("!");
                }
            }

            foreach (Cell cell in landSlot)
            {
                if (cell.cell_Contents.Contains(plants[1]))
                {
                    if ((gameTimer - newCellTimers[cell]) % plants[1].Shooting_Speed == 0)
                    {
                        cell.cell_Contents.Add("!");
                    }
                }
                if (cell.cell_Contents.Contains(plants[0]))
                {
                    if ((gameTimer - newCellTimers[cell]) % plants[0].Shooting_Speed == 0)
                    {
                        cell.cell_Contents.Add(projectiles[1]);
                    }
                }

                //Somehow only move every Projectile ONCE!
                if (cell.cell_Contents.Contains(projectiles[1]) && cell != lastMovedProj)
                {
                    int index = landSlot.FindIndex(a => a == cell);
                    Regex rgx = new Regex(@"\D+");
                    string indexRow = rgx.Match(landSlot[index].cell_ID).Value;
                    int nextValue = index + 1;
                    string nextValueRow = null;
                    if(nextValue < landSlot.Count)
                    {
                        nextValueRow = rgx.Match(landSlot[nextValue].cell_ID).Value;
                    }
                    landSlot[index].cell_Contents.Remove(projectiles[1]);

                    if(nextValueRow == indexRow && nextValue <= landSlot.Count)
                    {
                        landSlot[nextValue].cell_Contents.Add(projectiles[1]);
                        lastMovedProj = landSlot[nextValue];
                    }
                }

                if (cell.cell_Contents.Contains(zombies[0]) && gameTimer % zombies[0].speed == 0)
                {
                    int index = landSlot.FindIndex(a => a == cell);
                    Regex rgx = new Regex(@"\D+");
                    string indexRow = rgx.Match(landSlot[index].cell_ID).Value;
                    int nextValue = index - 1;
                    string nextValueRow = null;
                    if (nextValue > 0)
                    {
                        nextValueRow = rgx.Match(landSlot[nextValue].cell_ID).Value;
                    }
                    landSlot[index].cell_Contents.Remove(zombies[0]);

                    if (nextValueRow == indexRow && nextValue <= landSlot.Count)
                    {
                        landSlot[nextValue].cell_Contents.Add(zombies[0]);
                    }
                }
            }
            
            foreach(ZombieGenerationInfo enemyGen in zombGen)
            {
                if(gameTimer == enemyGen.spawnTime)
                {
                    foreach(Cell cell in landSlot)
                    {
                        if (cell.cell_ID.Contains(enemyGen.row) && cell.cell_ID.Contains("10"))
                        {
                            cell.cell_Contents.Add(enemyGen.zombieToGenerate);
                        }
                    }
                }
            }
        }
    }

    //Creating a plant + executing plant logic -- Unfinished, in fact some may say not even started lol
    internal class PlantBrain
    {
        public string Plant_Name;
        public string Plant_Description;
        public int Shooting_Speed;
        public int Sun_cost;
        public string symbol;
        public string color;

        public PlantBrain(string name, string desc, int sunCost, int shootSpeed, string plantSymbol, string plantColor)
        {
            Plant_Name = name;
            Plant_Description = desc;
            Sun_cost = sunCost;
            Shooting_Speed = shootSpeed;
            symbol = plantSymbol;
            color = plantColor;
        }
    }

    //Creating a zombie + executing plant logic -- Unfinished, in fact some may say not even started lol
    internal class ZombieBrain
    {
        public string Zombie_Name;
        public string Zombie_Description;
        public string symbol;
        public string color;
        public double speed;
        public ZombieBrain(string name, string desc, string zombSymbol, string zombColor, double zombieSpeed)
        {
            Zombie_Name = name;
            Zombie_Description = desc;
            symbol = zombSymbol;
            color = zombColor;
            speed = zombieSpeed;
        }
    }

    internal class Cell
    {
        public string cell_ID;
        public (int, int) cornerL;
        public (int, int) cornerR;
        public List<object> cell_Contents;

        //Id system variables
        char cellRow = 'A';
        int cellCol = 1;

        public Cell(string cell_id, (int, int) cellTopLeft, (int, int) cellBotRight, List<object> objCells)
        {
            cell_ID = cell_id;
            cornerL = cellTopLeft;
            cornerR = cellBotRight;
            cell_Contents = objCells;
        }

        public void AddToList(object itemToAdd)
        {
            cell_Contents.Add(itemToAdd);
        }
    }

    internal class Projectile
    {
        public string name;
        public int speed;
        public string symbol;
        public string color;

        public Projectile(string Projname, int projSpeed, string projSymbol, string projColor)
        {
            name = Projname;
            speed = projSpeed;
            symbol = projSymbol;
            color = projColor;
        }
    }

    internal class ZombieGenerationInfo
    {
        public int level;
        public ZombieBrain zombieToGenerate;
        public string row;
        public double spawnTime;
        public bool clearBoard;
        public int zombsDefeated;

        public ZombieGenerationInfo(int lvl, ZombieBrain zomb, string r, double timeToSpawn, bool clearBoardBeforeSpawn, int numOfZombs)
        {
            level = lvl;
            zombieToGenerate = zomb;
            row = r;
            spawnTime = timeToSpawn;
            clearBoard = clearBoardBeforeSpawn;
            zombsDefeated = numOfZombs;
        }
    }
}
