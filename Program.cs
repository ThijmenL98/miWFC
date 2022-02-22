using System;
using System.Windows.Forms;

namespace WFC4All {
    internal static class Program {
        public static Form1 form;

        [STAThread]
        // ReSharper disable once InconsistentNaming
        private static void Main() {
            // RunWfc();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            Application.Run(form);
        }

        // private static void RunWfc() {
        //     ITopoArray<int> sample = TopoArray.Create(new[] {
        //         new[] {1, 1, 1, 1},
        //         new[] {1, 2, 1, 1},
        //         new[] {1, 1, 1, 1},
        //         new[] {1, 1, 1, 1},
        //     }, true);
        //
        //     DeBroglie.Models.OverlappingModel model = new DeBroglie.Models.OverlappingModel(3);
        //     model.AddSample(sample.ToTiles());
        //     GridTopology topology = new GridTopology(10, 10, false);
        //     TilePropagator propagator = new TilePropagator(model, topology, new TilePropagatorOptions {
        //         BackTrackDepth = 0,
        //     });
        //     Resolution status = propagator.Run();
        //     if (status != Resolution.Decided) {
        //         throw new Exception("Undecided");
        //     }
        //
        //     ITopoArray<int> output = propagator.ToValueArray<int>();
        //     for (int y = 0; y < 10; y++) {
        //         for (int x = 0; x < 10; x++) {
        //             Console.Write(output.Get(x, y));
        //         }
        //
        //         Console.WriteLine();
        //     }
        // }
    }
}