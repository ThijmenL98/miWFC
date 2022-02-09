using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

namespace WFC4All {
    public class InputManager {
        public static Bitmap RunWfc(Form1 form) {
            Stopwatch sw = Stopwatch.StartNew();

            Random random = new Random();
            XDocument xDoc = XDocument.Load("samples.xml");

            XElement xElem = xDoc.Root.Elements("overlapping", "simpletiled").ElementAtOrDefault(0);

            Model model;
            string name = xElem.Get<string>("name");
            Console.WriteLine($@"< {name}");

            bool isOverlapping = xElem != null && xElem.Name == "overlapping";
            int size = xElem.Get("size", isOverlapping ? 48 : 24);
            int width = xElem.Get("width", size);
            int height = xElem.Get("height", size);
            
            bool periodic = xElem.Get("periodic", false);
            string heuristicString = xElem.Get<string>("heuristic");
            Model.Heuristic heuristic = heuristicString == "Scanline"
                ? Model.Heuristic.SCANLINE
                : heuristicString == "MRV"
                    ? Model.Heuristic.MRV
                    : Model.Heuristic.ENTROPY;

            if (isOverlapping) {
                int n = xElem.Get("N", 3);
                bool periodicInput = xElem.Get("periodicInput", true);
                int symmetry = xElem.Get("symmetry", 8);
                int ground = xElem.Get("ground", 0);

                model = new OverlappingModel(name, n, width, height, periodicInput, periodic, symmetry, ground,
                    heuristic, form);
            } else {
                string subset = xElem.Get<string>("subset");
                bool blackBackground = xElem.Get("blackBackground", false);

                model = new SimpleTiledModel(name, subset, width, height, periodic, blackBackground, heuristic, form);
            }

            for (int i = 0; i < xElem.Get("screenshots", 2); i++) {
                for (int k = 0; k < 10; k++) {
                    Console.Write(@"> ");
                    int seed = random.Next();
                    bool success = model.Run(seed, xElem.Get("limit", -1));

                    if (success) {
                        Console.WriteLine(@"DONE");

                        Console.WriteLine($@"time = {sw.ElapsedMilliseconds}");
                        return model.Graphics();
                    }

                    Console.WriteLine(@"CONTRADICTION");
                }
            }

            Console.WriteLine($@"time = {sw.ElapsedMilliseconds}");
            return null;
        }
    }
}