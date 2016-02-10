using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManyConsole;
using NLog;

namespace FreeNom
{
    public class AddCommand : ConsoleCommand
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public AddCommand()
        {
            this.IsCommand("add", "Add subdomains");
        }

        public override int Run(string[] remainingArguments)
        {
            logger.Info("Starting Add Domains");

            FreeNom update = new FreeNom();

            update.BeginAdd();

            logger.Info("Domains added, exiting");

            return 0;
        }
    }
}