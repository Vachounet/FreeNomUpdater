using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManyConsole;
using NLog;

namespace FreeNom
{
    public class UpdateCommand : ConsoleCommand
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public UpdateCommand()
        {
            this.IsCommand("update", "Update");
        }

        public override int Run(string[] remainingArguments)
        {
            logger.Info("Starting Update");

            FreeNom update = new FreeNom();

            update.BeginUpdate();

            logger.Info("Update Done, exiting");

            return 0;
        }
    }
}