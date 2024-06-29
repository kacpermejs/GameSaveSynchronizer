using GameSaveSynchronizerCore;

namespace GameSaveSynchronizerConsole {
    internal class Program {

        public static Synchronizer synchronizer = new Synchronizer();

        static void Main(string[] args) {
            synchronizer.Initialize();
        }
    }
}
