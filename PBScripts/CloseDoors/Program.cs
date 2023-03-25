using Sandbox.ModAPI.Ingame;

namespace PBScripts.CloseDoors
{
    /// <summary>
    /// Sample script that closes doors every 5 seconds.
    /// If the door was opened less than 5 seconds ago,
    /// it will wait an extra 5 seconds before closing it.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The constructor, called only once every session and
        /// always before any other method is called. Use it to
        /// initialize your script.
        ///
        /// The constructor is optional and can be removed if not
        /// needed.
        ///
        /// It's recommended to set RuntimeInfo.UpdateFrequency
        /// here, which will allow your script to run itself without a
        /// timer block.
        /// </summary>
        public Program()
        {
        }

        /// <summary>
        /// Called when the program needs to save its state. Use
        /// this method to save your state to the Storage field
        /// or some other means.
        ///
        /// This method is optional and can be removed if not
        /// needed.
        /// </summary>
        public void Save()
        {
        }

        /// <summary>
        /// The main entry point of the script, invoked every time
        /// one of the programmable block's Run actions are invoked,
        /// or the script updates itself. The updateSource argument
        /// describes where the update came from.
        ///
        /// The method itself is required, but the arguments above
        /// can be removed if not needed.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="updateSource"></param>

        public void Main(string argument, UpdateType updateSource)
        {
        }
    }
}