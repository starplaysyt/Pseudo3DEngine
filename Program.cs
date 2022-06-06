using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Pseudo3DEngine
{
    class Program
    {
        //** Configuration Unit **//
        private const int ScreenHeight = 50; 
        private const int ScreenWidth = 100;

        private const int MapWidth = 32;
        private const int MapHeight = 32;

        private const double fov = Math.PI / 3;
        private const double depth = 16;

        private static double PlayerX = 8;
        private static double PlayerY = 8;
        private static double PlayerA = 0;
        //** End of Configuration Unit **//

        private static readonly StringBuilder Map = new StringBuilder();

        public static readonly char[] Screen = new char[ScreenWidth*ScreenHeight];
        static async Task Main(string[] args)
        {
            Console.SetWindowSize(ScreenWidth, ScreenHeight);
            Console.CursorVisible = false;
            Console.SetBufferSize(ScreenWidth, ScreenHeight);
            
            InitMap();

            DateTime datetimefrom = DateTime.Now;
            while (true)
            {
                DateTime datetimeto = DateTime.Now;
                double elapsedTime = (datetimeto - datetimefrom).TotalSeconds;
                datetimefrom = DateTime.Now;
                //PlayerA += 0.5*elapsedTime;

                if (Console.KeyAvailable)
                {
                    var consoleKey = Console.ReadKey((true)).Key;
                    switch (consoleKey)
                    {
                        case ConsoleKey.A:
                            PlayerA += 5 * elapsedTime;
                            break;
                        case ConsoleKey.D:
                            PlayerA -= 5 * elapsedTime; break;
                        case ConsoleKey.W:
                            PlayerX += Math.Sin(PlayerA)* 10 * elapsedTime;
                            PlayerY += Math.Cos(PlayerA) * 10 *  elapsedTime;
                            if (Map[(int)PlayerY * MapWidth + (int)PlayerX] == '#')
                            {
                                PlayerX -= Math.Sin(PlayerA)* 10 * elapsedTime;
                                PlayerY -= Math.Cos(PlayerA) * 10 *  elapsedTime;
                            }
                            break;
                        case ConsoleKey.S:
                            PlayerX -= Math.Sin(PlayerA)* 10 * elapsedTime;
                            PlayerY -= Math.Cos(PlayerA) * 10 *  elapsedTime;
                            if (Map[(int)PlayerY * MapWidth + (int)PlayerX] == '#')
                            {
                                PlayerX += Math.Sin(PlayerA)* 10 * elapsedTime;
                                PlayerY += Math.Cos(PlayerA) * 10 *  elapsedTime;
                            }
                            break;
                    }
                    
                    InitMap();
                }

                var rayCastingTasks = new List<Task<Dictionary<int, char>>>();

                for (int i = 0; i < ScreenWidth; i++)
                {
                    int i1 = i;
                    rayCastingTasks.Add(Task.Run(() => Casting((i1))));
                }

                var rays = await Task.WhenAll(rayCastingTasks);

                foreach (var dictionary in rays)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        Screen[key] = dictionary[key];
                    }
                }

                char[] stats = $"X: {PlayerX}, Y: {PlayerY}, A: {PlayerA}, FPS: {(int)1/elapsedTime}".ToCharArray();
                stats.CopyTo(Screen, 0);

                /*for (int i = 0; i < MapWidth; i++)
                {
                    for (int j = 0; j < MapHeight; j++)
                    {
                        Screen[(j + 1) * ScreenWidth + i] = _map[j * i];
                    }
                }

                Screen[(int) (PlayerY + 1) * ScreenWidth + (int) PlayerX] = 'P';*/
                
                
                
                Console.SetCursorPosition(0, 0);
                Console.Write(Screen);
            } 
        }

        public static Dictionary<int, char> Casting(int i)
        {
            var result = new Dictionary<int, char>();
            double rayAngle = PlayerA + fov / 2 - i * fov / ScreenWidth;

                    double rayX = Math.Sin(rayAngle);
                    double rayY = Math.Cos(rayAngle);
                    double distToWall = 0;
                    bool hitwall = false;
                    bool isBounds = false;

                    while (!hitwall && distToWall < depth)
                    {
                        distToWall += 0.1;
                        int traceX = (int) (PlayerX + rayX * distToWall);
                        int traceY = (int) (PlayerY + rayY * distToWall);
                        if (traceX < 0 || traceX >= depth + PlayerX || traceY < 0 || traceY >= depth + PlayerY)
                        {
                            hitwall = true;
                            distToWall = depth;
                        }
                        else
                        {
                            char testCell = Map[traceY * MapWidth + traceX];
                            if (testCell == '#')
                            {
                                hitwall = true;

                                var boundsVectorList = new List<(double module, double cos)>();
                                for (int tx = 0; tx < 2; tx++)
                                {
                                    for (int ty = 0; ty < 2; ty++)
                                    {
                                        double vx = traceX + tx - PlayerX;
                                        double vy = traceY + ty - PlayerY;
                                        double vectorModule = Math.Sqrt(vx*vx+vy*vy);
                                        double cosAngle = rayX * vx / vectorModule + rayY * vy / vectorModule;
                                        boundsVectorList.Add((vectorModule, cosAngle));
                                    }
                                }

                                boundsVectorList = boundsVectorList.OrderBy(v => v.module).ToList();

                                double BoundAngle = 0.03/ distToWall;
                                if (Math.Acos(boundsVectorList[0].cos) < BoundAngle || Math.Acos(boundsVectorList[1].cos) < BoundAngle)
                                {
                                    isBounds = true;
                                }
                            }
                            else
                            {
                                Map[traceY * MapWidth + traceX] = '*';
                            }
                        }
                    }

                    int celling = (int) (ScreenHeight / 2d - ScreenHeight * fov / distToWall);
                    int floor = ScreenHeight - celling;

                    char wallShade;

                    if (isBounds)
                        wallShade = '|';
                    else if (distToWall < depth / 4d)
                    {
                        wallShade = '\u2588';
                    }
                    else if (distToWall < depth / 3d)
                    {
                        wallShade = '\u2593';
                    }
                    else if (distToWall < depth / 2d)
                    {
                        wallShade = '\u2592';
                    }
                    else if (distToWall < depth)
                    {
                        wallShade = '\u2591';
                    }
                    else wallShade = ' ';

                    for (int j = 0; j < ScreenHeight; j++)
                    {
                        if (j <= celling)
                        {
                            result[j * ScreenWidth + i] = ' ';
                        }
                        else if (j >= celling && j <= floor)
                        {
                            result[j * ScreenWidth + i] = wallShade;
                        }
                        else
                        {
                            char floorshade;
                            double b = 1 - (j - ScreenHeight / 2d) / (ScreenHeight / 2d);
                            if (b < 0.25)
                            {
                                floorshade = '#';
                            }
                            else if (b < 0.5)
                            {
                                floorshade = 'x';
                            }
                            else if (b < 0.75)
                                floorshade = '-';
                            else if (b < 0.9)
                                floorshade = '.';
                            else floorshade = ' ';

                            result[j * ScreenWidth + i] = floorshade;
                        }
                    }

                    return result;
        }
        public static void InitMap()
        {
            Map.Clear();
            Map.Append("################################");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#....######....................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
        }
    }
}
