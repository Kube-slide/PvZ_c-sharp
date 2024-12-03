using System;
using System.Drawing;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace PVZ_console
{
    internal class Program
    {
        //global variables | Organized as best as my little brain can

        //Entity lists
        static string notesFilePath = @"..\..\..\Entities.txt";
        static string zombGeneration = @"..\..\..\ZombieGeneration.txt";
        static List<PlantBrain> plants = new List<PlantBrain>();
        public static List<ZombieBrain> zombies = new List<ZombieBrain>();
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
        static string[] gameStates = { "Menu", "In-game", "Paused", "Instructions", "Credits", "Quit", "Game over", "Almanac"};
        static string curState = gameStates[0];
        static string prevState = null;
        static object lastMovedProj = null;

        //Conditional checks for loading
        static bool generatedCells = false;
        static bool preload = false;
        static int selectedOption = 0;

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
                Console.CursorVisible = false;
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
            Console.SetCursorPosition(0, 0);
            Console.Clear();
            Main(null);
        }

        //Force screen size to display everything adequatly
        static void ScreenSize()
        {
            int desiredH = 30;

            bool FixedWindow = (Console.WindowHeight != 30);
            while (FixedWindow)
            {
                string[] warningMessages = {$"To play this game, your screen must be set to an exact height of {desiredH}", $"Currently, the height of your console is {Console.WindowHeight}",
                    "Make adjustments to the window, then press any key to check again", "WARNING : SCROLLING TO ADJUST TEXT SIZE WILL STOP THE GAME AND BRING YOU BACK TO THIS SCREEN!" };

                Console.SetCursorPosition((Console.WindowWidth / 2), (Console.WindowHeight / 5));

                for (int i = 0; i < warningMessages.Length; i++)
                {
                    Console.Write("\n");
                    Console.CursorLeft = (Console.WindowWidth / 2) - (warningMessages[i].Length / 2);
                    Console.Write(warningMessages[i]);
                    Console.Write("\n");
                }


                Console.ReadKey(true);
                FixedWindow = (Console.WindowHeight != desiredH);
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
                        plants.Add(new PlantBrain(subStrings[0], subStrings[1], Convert.ToInt32(subStrings[2]), Convert.ToInt32(subStrings[3]), subStrings[4], subStrings[5], Convert.ToInt16(subStrings[6])));
                    }
                    else if (line.StartsWith("z_"))
                    {
                        //Break string into pieces --> store the info in an array and create a zombie from given data
                        string[] subStrings = line.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
                        zombies.Add(new ZombieBrain(subStrings[0], subStrings[1], subStrings[2], subStrings[3], Convert.ToDouble(subStrings[4]), Convert.ToInt16(subStrings[5]), Convert.ToInt16(subStrings[6])));
                    }
                    else if (line.StartsWith("s_"))
                    {
                        //Break string into pieces --> store the info in an array and create a zombie from given data
                        string[] subStrings = line.Split("|", StringSplitOptions.RemoveEmptyEntries);
                        projectiles.Add(new Projectile(subStrings[0], Convert.ToInt32(subStrings[1]), subStrings[2], subStrings[3]));
                    }
                }
            }

            using (var sr = new StreamReader(zombGeneration))
            {
                while (sr.Peek() >= 0)
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
                                zombToAdd = (ZombieBrain)zomb.Clone();
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
                    DPM();
                    break;
                case "Instructions":
                    DIM();
                    break;
                case "Game over":
                    DGO();
                    break;
                case "Quit":
                    DQG();
                    break;
                case "Credits":
                    DGC();
                    break;
                case "Almanac":
                    DAM();
                    break;
                default:
                    Console.WriteLine("ERROR; PLEASE RESTART THE GAME");
                    break;
            }
            //End of frame--> accept player inputs
            PlayerInputs();
        }

        //Draw main menu
        static void DMM()
        {
            (int, int) windowSize = (Console.WindowWidth, Console.WindowHeight);

            string titleCard = "---=== Plants vs. Zombies : Console app edition ===---";

            Console.SetCursorPosition((windowSize.Item1 / 2) - (titleCard.Length / 2), (windowSize.Item2 / 4));

            Console.WriteLine(titleCard + "\n");

            string[] gameOptions = {"1. Play game", "2. Instructions", "3. Credits", "4. View Almanac", "5. Exit to desktop", "", "Write your choice # or use up arrow and down arrow to navigate", "Press enter to select an option"};

            for (int i = 0; i < gameOptions.Length; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (windowSize.Item1 / 2) - (gameOptions[i].Length / 2);
                if(i == selectedOption)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(gameOptions[i] + "   <---");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(gameOptions[i]);
                }
                
            }

            Console.SetCursorPosition((Console.WindowWidth / 2), Console.WindowTop + Console.WindowHeight - 10);
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
                        var displayedChar = (cell_list.Find(Cell => Cell.cell_ID == curId).cell_Contents.LastOrDefault());

                        if (displayedChar.GetType().Name == "PlantBrain")
                        {
                            Console.ResetColor();
                            Console.Write("║");
                            Console.ForegroundColor = SetConsoleColor(((PlantBrain)displayedChar).color);
                            Console.Write(((PlantBrain)displayedChar).symbol);
                            Console.ResetColor();
                            Console.Write("║");
                        }


                        if (displayedChar.GetType().Name == "ZombieBrain")
                        {
                            Console.ResetColor();
                            Console.Write("║");
                            Console.ForegroundColor = SetConsoleColor(((ZombieBrain)displayedChar).color);
                            Console.Write(((ZombieBrain)displayedChar).symbol);
                            Console.ResetColor();
                            Console.Write("║");
                        }

                        if (displayedChar.GetType().Name == "Projectile")
                        {
                            Console.ResetColor();
                            Console.Write("║");
                            Console.ForegroundColor = SetConsoleColor(((Projectile)displayedChar).color);
                            Console.Write(((Projectile)displayedChar).symbol);
                            Console.ResetColor();
                            Console.Write("║");
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
                        if (displayedChar == "-")
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

        //Draws information about cells in-game
        static void CellInfo(Cell cellToShow)
        {
            Console.SetCursorPosition(Console.WindowLeft + Console.WindowWidth - 50, Console.WindowTop + 5);
            List<string> cellInfo = new List<string>();

            cellInfo.Add("This cell contains : ");

            foreach(object obj in cellToShow.cell_Contents)
            {
                if (landSlot.Contains(cellToShow))
                {
                    if (obj is ZombieBrain)
                    {
                        ZombieBrain z = (ZombieBrain)obj;
                        cellInfo.Add($"{z.Zombie_Name}. Remaining HP : {z.hp}");
                    }
                    else if (obj is PlantBrain)
                    {
                        PlantBrain p = (PlantBrain)obj;
                        cellInfo.Add($"{p.Plant_Name}. Remaining HP : {p.hp}");
                    }
                }
                if (seedSlot.Contains(cellToShow))
                {
                    if (obj is PlantBrain)
                    {
                        PlantBrain p = (PlantBrain)obj;
                        cellInfo.Add($"Seed slot containing {p.Plant_Name}. Sun cost : {p.Sun_cost}.");
                    }
                }
            }

            for (int i = 0; i < cellInfo.Count; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (Console.WindowLeft + Console.WindowWidth - 50) - (cellInfo[i].Length / 2);
                Console.Write(cellInfo[i]);
            }

        }

        //Draw pause menu
        static void DPM()
        {
            (int, int) consoleSize = (Console.WindowWidth, Console.WindowHeight);

            string[] message = {"---PAUSED---", "", "Press ENTER to resume game", "", "Press ESCAPE to exit to desktop"};

            for (int i = 0; i < message.Length; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (consoleSize.Item1 / 2) - (message[i].Length / 2);
                Console.Write(message[i]);
            }
        }

        //Draw instructions menu
        static void DIM()
        {
            (int, int) consoleSize = (Console.WindowWidth, Console.WindowHeight);

            string[] message = {"-------HOW TO PLAY------", "Zombies are trying to invade your lawn! You need to stop them before they eat your brains!", "-----Understanding how the game looks-----", "The top row of cells is for your seeds",
            "Cells are the corner stone of plants VS zombies. They look like this :", "╔═╗", "║ ║", "╚═╝", "Each cell can contain a number of things, all represented by symbols and letters!", "Here are a few of the important ones :", 
            "The first cell contains a number : This is your sun, the currency for plants!", "An exclamation point [!] signifies that there is sun in that cell! You can pick that up and collect it!",
            "The next cells in the first row contain plants! Purchase plants with sun to protect you.", "The second row is incassesible. You can ignore it!", "The third row onward is your playing field!",
            "It's in these rows that the game takes place. You will be able to plant in those rows, as well as see the zombies attack", "-----Gameplay-----",
            "Each plant has its own feature, health and sun cost. Check them out in the Almanac!", "Watch out! You aren't the only one who can attack...",
            "Each Zombie also has special properties! You can also view them in the Almanac!", "-----Controls-----", "To pick up plants, simply hover over the plant in the first row and press space bar to pick them up!",
            "To plant it, hover over any cell that DOES NOT already contain a plant, and press space again!", "You can always check if you have a plant by looking at the bottom of the game screen!", "" ,"Press any key to return to the main Menu"};

            for (int i = 0; i < message.Length; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (consoleSize.Item1 / 2) - (message[i].Length / 2);
                Console.Write(message[i]);
            }
        }

        //Draw game over
        static void DGO()
        {
            (int, int) consoleSize = (Console.WindowWidth, Console.WindowHeight);

            string[] message = { "---GAME OVER---", "The zombies ate your brains...", "", "FINAL SCORE : LEVEL X", "ZOMBIES DEFEATED : X ZOMBIES", "", "Press ENTER to return to main menu and try again", "", "Press ESCAPE to exit to desktop" };

            for (int i = 0; i < message.Length; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (consoleSize.Item1 / 2) - (message[i].Length / 2);
                Console.Write(message[i]);
            }
        }

        //Draw quit game
        static void DQG()
        {
            (int, int) consoleSize = (Console.WindowWidth, Console.WindowHeight);
            string[] message = { "Are you sure you want to quit? [Y/N]", "All progress will be lost upon exiting!"};
            string userChoice;

            do
            {

                for (int i = 0; i < message.Length; i++)
                {
                    Console.Write("\n");
                    Console.CursorLeft = (consoleSize.Item1 / 2) - (message[i].Length / 2);
                    Console.Write(message[i]);
                }

                Console.SetCursorPosition((consoleSize.Item1 / 2), Console.WindowTop + (Console.WindowHeight - 10));

                userChoice = Console.ReadLine();

                if (userChoice.ToUpper() == "Y")
                {
                    System.Environment.Exit(1);
                }
                else
                {
                    curState = prevState;
                }
            } while (userChoice.ToUpper() != "Y" && userChoice.ToUpper() != "N");
        }

        //Draw game credits
        static void DGC()
        {
            (int, int) consoleSize = (Console.WindowWidth, Console.WindowHeight);

            string[] message = {"Game by : Abdul Raja", "Original concept : Plants vs. Zombies by EA & Popcap", "All code is available under the MIT liscence on github [user : kube-slide]", "", "Press any key to return to Main Menu"};

            for (int i = 0; i < message.Length; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (consoleSize.Item1 / 2) - (message[i].Length / 2);
                Console.Write(message[i]);
            }
        }

        //Draw almanac (list of plants and zombies)
        static void DAM()
        {
            (int, int) windowSize = (Console.WindowWidth, Console.WindowHeight);

            Console.SetCursorPosition((windowSize.Item1 / 2), Console.WindowTop);

            List<string> almanacInfo = new List<string>{ "-----The Almanac-----", "", "--- Plants ---"};

            for(int i = 0; i < plants.Count; i++)
            {
                string plantInfo = $"{plants[i].Plant_Name} : {plants[i].Plant_Description}. Symbol = {plants[i].symbol}";
                almanacInfo.Add(plantInfo);
            }

            almanacInfo.Add("");
            almanacInfo.Add("--- Zombies ---");

            for(int i = 0; i < zombies.Count; i++)
            {
                string zombInfo = $"{zombies[i].Zombie_Name} : {zombies[i].Zombie_Description}. Symbol = {zombies[i].symbol}";
                almanacInfo.Add(zombInfo);
            }

            almanacInfo.Add("");
            almanacInfo.Add("Press any key to return to main menu");

            for (int i = 0; i < almanacInfo.Count; i++)
            {
                Console.Write("\n");
                Console.CursorLeft = (windowSize.Item1 / 2) - (almanacInfo[i].Length / 2);
                Console.Write(almanacInfo[i]);
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
                //Check for input WITHOUT blocking script. W/o console.KeyAvailable, the rest of the code would hang :\

                case "In-game":
                    if (Console.KeyAvailable && isInWindow)
                    {
                        switch (Console.ReadKey().Key)
                        {
                            case ConsoleKey.Spacebar:
                                for (int i = 0; i < cell_list.Count; i++)
                                {
                                    if (IsInCell(convertedCharLength, cell_list[i], MousePos))
                                    {
                                        DoCellAction(cell_list[i]);
                                        break;
                                    }
                                }
                                break;
                            case ConsoleKey.Escape:
                                prevState = curState;
                                curState = "Paused";
                                break;

                        }
                    }

                    if (!isInWindow)
                    {
                        prevState = curState;
                        curState = "Paused";
                    }

                    break;

                case "Menu":
                    if (Console.KeyAvailable)
                    {
                        switch (Console.ReadKey().Key)
                        {
                            case ConsoleKey.D1:
                                prevState = curState;
                                curState = gameStates[1];
                                break;
                            case ConsoleKey.D2:
                                prevState = curState;
                                curState = gameStates[3];
                                break;
                            case ConsoleKey.D3:
                                prevState = curState;
                                curState = gameStates[4];
                                break;
                            case ConsoleKey.D4:
                                prevState = curState;
                                curState = gameStates[7];
                                break;
                            case ConsoleKey.D5:
                            case ConsoleKey.Escape:
                                prevState = curState;
                                curState = gameStates[5];
                                break;
                            case ConsoleKey.DownArrow:
                                if(selectedOption + 1 > 4)
                                {
                                    selectedOption = 0;
                                }
                                else
                                {
                                    selectedOption++;
                                }
                                break;
                            case ConsoleKey.UpArrow:
                                if (selectedOption - 1 < 0)
                                {
                                    selectedOption = 4;
                                }
                                else
                                {
                                    selectedOption--;
                                }
                                break;
                            case ConsoleKey.Enter:
                                switch (selectedOption)
                                {
                                    case 0:
                                        prevState = curState;
                                        curState = gameStates[1];
                                        break;
                                    case 1:
                                        prevState = curState;
                                        curState = gameStates[3];
                                        break;
                                    case 2:
                                        prevState = curState;
                                        curState = gameStates[4];
                                        break;
                                    case 3:
                                        prevState = curState;
                                        curState = gameStates[7];
                                        break;
                                    case 4:
                                        prevState = curState;
                                        curState = gameStates[5];
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case "Instructions":
                    if (Console.KeyAvailable)
                    {
                        switch (Console.ReadKey().Key)
                        {
                            default:
                                prevState = curState;
                                curState = gameStates[0];
                                break;
                        }
                    }
                    break;

                case "Paused":
                    if (Console.KeyAvailable)
                    {
                        switch (Console.ReadKey().Key)
                        {
                            case ConsoleKey.Enter:
                                prevState = curState;
                                curState = "In-game";
                                break;
                            case ConsoleKey.Escape:
                                prevState = curState;
                                curState = "Quit";
                                break;
                        }
                    }
                    break;

                case "Credits":
                    if (Console.KeyAvailable)
                    {
                        if(Console.ReadKey().Key != null)
                        {
                            curState = prevState;
                        }
                    }
                    break;
                
                case "Almanac":
                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key != null)
                        {
                            curState = prevState;
                        }
                    }
                    break;

                case "Game over":
                    if (Console.KeyAvailable)
                    {
                        switch (Console.ReadKey().Key)
                        {
                            case ConsoleKey.Enter:
                                prevState = curState;
                                curState = "Menu";
                                break;
                            case ConsoleKey.Escape:
                                prevState = curState;
                                curState = "Quit";
                                break;
                        }
                    }
                    break;

            }

            if (!isInWindow)
            {
                string message = "Outside game area! Return back to keep playing!";
                Console.SetCursorPosition(Console.WindowWidth / 2 - (message.Length / 2), Console.WindowTop + Console.WindowHeight - 5);
                Console.WriteLine(message);
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
                if (mouseQueue != null && cellChecked.cell_Contents.OfType<PlantBrain>().ToList().Count == 0)
                {
                    cellChecked.cell_Contents.Add(((PlantBrain)mouseQueue).Clone());
                    mouseQueue = null;

                    newCellTimers.Add(cellChecked, gameTimer);
                    return;
                }
            }

            CellInfo(cellChecked);
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
                if (cell.cell_Contents.OfType<PlantBrain>().ToList().Count > 0)
                {
                    PlantBrain plantInCell = cell.cell_Contents.OfType<PlantBrain>().ToList()[0]; //Retrives the plant in the cell

                    //Kill the plant if they have no more hp ; Do this before plant logic to avoid plants being "alive" after death
                    if (plantInCell.hp <= 0)
                    {
                        cell.cell_Contents.Remove(plantInCell);
                    }

                    //peashooter
                    if (plantInCell.symbol == plants[0].symbol)
                    {
                        bool alreadyShot = false;
                        if ((gameTimer - newCellTimers[cell]) % plants[0].Shooting_Speed == 0 && !alreadyShot)
                        {
                            Regex rgx = new Regex(@"\D");
                            string cellLetter = rgx.Match(cell.cell_ID).Value;
                            for (int i = landSlot.IndexOf(cell); i < landSlot.Count; i++)
                            {
                                if (landSlot[i].cell_ID.Contains(cellLetter))
                                {
                                    foreach (ZombieBrain zomb in zombies)
                                    {
                                        if (landSlot[i].cell_Contents.OfType<ZombieBrain>().Any() && !alreadyShot)
                                        {
                                            cell.cell_Contents.Add(projectiles[1]);
                                            alreadyShot = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //sunflower
                    if (plantInCell.symbol == plants[1].symbol)
                    {
                        if ((gameTimer - newCellTimers[cell]) % plants[1].Shooting_Speed == 0)
                        {
                            cell.cell_Contents.Add("!");
                        }
                    }

                    //Potato mine ; Not armed yet
                    if(plantInCell.symbol == plants[3].symbol)
                    {
                        if((gameTimer - newCellTimers[cell]) % plants[3].Shooting_Speed == 0)
                        {
                            plantInCell.symbol = "O";
                        }
                    }

                    //Potato mine ; Armed status
                    if(plantInCell.symbol == "O")
                    {
                        if (cell.cell_Contents.OfType<ZombieBrain>().Any())
                        {
                            List<ZombieBrain> zombiesInCell = cell.cell_Contents.OfType<ZombieBrain>().ToList();
                            cell.cell_Contents.Remove(zombiesInCell[0]);
                            cell.cell_Contents.Remove(cell.cell_Contents.OfType<PlantBrain>().ToList()[0]);
                        }
                    }

                }

                if (cell.cell_Contents.Contains(projectiles[1]) && cell != lastMovedProj)
                {
                    int index = landSlot.FindIndex(a => a == cell);
                    Regex rgx = new Regex(@"\D+");
                    string indexRow = rgx.Match(landSlot[index].cell_ID).Value;
                    int nextValue = index + 1;

                    if (nextValue >= landSlot.Count)
                        nextValue = landSlot.Count - 1;

                    string nextValueRow = null;
                    if (nextValue < landSlot.Count)
                    {
                        nextValueRow = rgx.Match(landSlot[nextValue].cell_ID).Value;
                    }

                    if (landSlot[index].cell_Contents.OfType<ZombieBrain>().Any())
                    {
                        var targetZombie = landSlot[index].cell_Contents.LastOrDefault(z => z is ZombieBrain);

                        if (targetZombie != null)
                        {
                            ((ZombieBrain)targetZombie).hp--;
                            landSlot[index].cell_Contents.Remove(projectiles[1]);
                            landSlot[nextValue].cell_Contents.Remove(projectiles[1]);
                        }
                        break;
                    }
                    if (landSlot[nextValue].cell_Contents.OfType<ZombieBrain>().Any())
                    {
                        var targetZombie = landSlot[nextValue].cell_Contents.LastOrDefault(z => z is ZombieBrain);

                        if (targetZombie != null)
                        {
                            ((ZombieBrain)targetZombie).hp--;
                            landSlot[index].cell_Contents.Remove(projectiles[1]);
                            landSlot[nextValue].cell_Contents.Remove(projectiles[1]);
                        }
                        break;
                    }

                    landSlot[index].cell_Contents.Remove(projectiles[1]);

                    if (nextValueRow == indexRow && nextValue <= landSlot.Count)
                    {
                        landSlot[nextValue].cell_Contents.Add(projectiles[1]);
                        lastMovedProj = landSlot[nextValue];
                    }
                }

                if (cell.cell_Contents.OfType<ZombieBrain>().ToList().Count > 0)
                {
                    List<ZombieBrain> zombiesInCell = cell.cell_Contents.OfType<ZombieBrain>().ToList();

                    foreach (ZombieBrain zombs in zombiesInCell)
                    {

                        //These conditions apply to all zombies anyways, might as well check them now
                        int index = landSlot.FindIndex(a => a == cell);
                        Regex rgx = new Regex(@"\D+");
                        string indexRow = rgx.Match(landSlot[index].cell_ID).Value;
                        int nextValue = index - 1;
                        string nextValueRow = null;


                        if (nextValue > 0)
                        {
                            nextValueRow = rgx.Match(landSlot[nextValue].cell_ID).Value;
                        }

                        if (nextValueRow != indexRow)
                        {
                            prevState = curState;
                            curState = "Game over";
                            ResetGame();
                            return;
                        }

                        if (zombs.hp <= 0)
                        {
                            cell.cell_Contents.Remove(zombs);
                        }

                        bool nextSlotHasPlant = landSlot[nextValue].cell_Contents.OfType<PlantBrain>().ToList().Count > 0;
                        bool currentSlotHasPlant = landSlot[index].cell_Contents.OfType<PlantBrain>().ToList().Count > 0;

                        if (nextSlotHasPlant)
                        {
                            if (landSlot[nextValue].cell_Contents.OfType<PlantBrain>().ToList()[0].symbol == "O")
                            {
                                nextSlotHasPlant = false;
                            }
                        }

                        //Regular zombies ; No special stuff going on
                        if (zombs.symbol == zombies[0].symbol || zombs.symbol == zombies[2].symbol || zombs.symbol == zombies[3].symbol || zombs.symbol == zombies[4].symbol)
                        {
                            if (nextSlotHasPlant && gameTimer % zombs.attackSpeed == 0)
                            {
                                --landSlot[nextValue].cell_Contents.OfType<PlantBrain>().ToList()[0].hp;
                            }
                            if (currentSlotHasPlant && gameTimer % zombs.attackSpeed == 0)
                            {
                                --landSlot[index].cell_Contents.OfType<PlantBrain>().ToList()[0].hp;
                            }

                            if (gameTimer % zombs.speed == 0 && !nextSlotHasPlant && !currentSlotHasPlant)
                            {
                                landSlot[index].cell_Contents.Remove(zombs);

                                if (nextValueRow == indexRow && nextValue <= landSlot.Count)
                                {
                                    landSlot[nextValue].cell_Contents.Add(zombs);
                                }
                            }
                        }

                        if (zombs.symbol == zombies[1].symbol)
                        {
                            if ((nextSlotHasPlant) && gameTimer % zombs.attackSpeed == 0)
                            {
                                    --landSlot[nextValue].cell_Contents.OfType<PlantBrain>().ToList()[0].hp;
                            }
                            if ((currentSlotHasPlant) && gameTimer % zombs.attackSpeed == 0)
                            {
                                --landSlot[index].cell_Contents.OfType<PlantBrain>().ToList()[0].hp;
                            }

                            if (gameTimer % zombs.speed == 0)
                            {
                                landSlot[index].cell_Contents.Remove(zombs);

                                if (nextValueRow == indexRow && nextValue <= landSlot.Count)
                                {
                                    if (!zombs.specialAction && (nextSlotHasPlant || currentSlotHasPlant))
                                    {
                                        landSlot[--nextValue].cell_Contents.Add(zombs);
                                        zombs.specialAction = true;
                                        zombs.speed = zombies[0].speed;
                                    }
                                    else if (zombs.specialAction || (!nextSlotHasPlant || !currentSlotHasPlant))
                                    {
                                        landSlot[nextValue].cell_Contents.Add(zombs);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (ZombieGenerationInfo enemyGen in zombGen)
            {
                if (gameTimer == enemyGen.spawnTime)
                {
                    foreach (Cell cell in landSlot)
                    {
                        if (cell.cell_ID.Contains(enemyGen.row) && cell.cell_ID.Contains("10"))
                        {
                            ZombieBrain zombToAdd = enemyGen.zombieToGenerate;
                            cell.cell_Contents.Add((ZombieBrain)zombToAdd);
                        }
                    }
                }
            }
        }
    
        static void ResetGame()
        {
            gameTimer = 0;
            sunQTY = 0;
            foreach(Cell cell in landSlot)
            {
                cell.cell_Contents.Clear();
                cell.cell_Contents.Add(" ");
            }
        }
    }

    internal class PlantBrain : ICloneable
    {
        public string Plant_Name;
        public string Plant_Description;
        public int Shooting_Speed;
        public int Sun_cost;
        public string symbol;
        public string color;
        public int hp;

        public PlantBrain(string name, string desc, int sunCost, int shootSpeed, string plantSymbol, string plantColor, int health)
        {
            Plant_Name = name;
            Plant_Description = desc;
            Sun_cost = sunCost;
            Shooting_Speed = shootSpeed;
            symbol = plantSymbol;
            color = plantColor;
            hp = health;
        }

        public object Clone()
        {
            PlantBrain clone = new PlantBrain(null, null, 0, 0, null, null, 0);
            clone.Plant_Name = this.Plant_Name;
            clone.Plant_Description = this.Plant_Description;
            clone.Shooting_Speed = this.Shooting_Speed;
            clone.Sun_cost = this.Sun_cost;
            clone.symbol = this.symbol;
            clone.color = this.color;
            clone.hp = this.hp;
            return clone;
        }
    }

    internal class ZombieBrain : ICloneable
    {
        public string Zombie_Name;
        public string Zombie_Description;
        public string symbol;
        public string color;
        public double speed;
        public int hp;
        public double attackSpeed;
        public bool specialAction = false;
        public ZombieBrain(string name, string desc, string zombSymbol, string zombColor, double zombieSpeed, int health, double atkSpd)
        {
            Zombie_Name = name;
            Zombie_Description = desc;
            symbol = zombSymbol;
            color = zombColor;
            speed = zombieSpeed;
            hp = health;
            attackSpeed = atkSpd;
        }

        public object Clone()
        {
            ZombieBrain clone = new ZombieBrain(null, null, null, null, 0, 0, 0);
            clone.Zombie_Name = this.Zombie_Name;
            clone.Zombie_Description = this.Zombie_Description;
            clone.symbol = this.symbol;
            clone.color = this.color;
            clone.speed = this.speed;
            clone.hp = this.hp;
            clone.attackSpeed = this.attackSpeed;
            return clone;
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
