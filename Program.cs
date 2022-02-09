using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WFC4All
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        
        static void RunWfc()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new Random();
            XDocument xdoc = XDocument.Load("samples.xml");

            foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
            {
                Model model;
                string name = xelem.Get<string>("name");
                Console.WriteLine($"< {name}");

                bool isOverlapping = xelem.Name == "overlapping";
                int size = xelem.Get("size", isOverlapping ? 48 : 24);
                int width = xelem.Get("width", size);
                int height = xelem.Get("height", size);
                bool periodic = xelem.Get("periodic", false);
                string heuristicString = xelem.Get<string>("heuristic");
                var heuristic = heuristicString == "Scanline" ? Model.Heuristic.Scanline : (heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy);

                if (isOverlapping)
                {
                    int N = xelem.Get("N", 3);
                    bool periodicInput = xelem.Get("periodicInput", true);
                    int symmetry = xelem.Get("symmetry", 8);
                    int ground = xelem.Get("ground", 0);

                    model = new OverlappingModel(name, N, width, height, periodicInput, periodic, symmetry, ground, heuristic);
                }
                else
                {
                    string subset = xelem.Get<string>("subset");
                    bool blackBackground = xelem.Get("blackBackground", false);

                    model = new SimpleTiledModel(name, subset, width, height, periodic, blackBackground, heuristic);
                }

                for (int i = 0; i < xelem.Get("screenshots", 2); i++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        Console.Write("> ");
                        int seed = random.Next();
                        bool success = model.Run(seed, xelem.Get("limit", -1));
                        if (success)
                        {
                            Console.WriteLine("DONE");
                            model.Graphics().Save($"output/{name} {seed}.png");
                            if (model is SimpleTiledModel stmodel && xelem.Get("textOutput", false)) System.IO.File.WriteAllText($"{name} {seed}.txt", stmodel.TextOutput());
                            break;
                        }
                        else Console.WriteLine("CONTRADICTION");
                    }
                }
            }

            Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
        }
    }
}