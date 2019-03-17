﻿using ADK.Demo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planetarium.ViewModels
{
    public class EphemerisVM : ViewModelBase
    {
        public DataTable EphemerisTable { get; private set; }
        public Command SaveToFileCommand { get; private set; }
        public Command CloseCommand { get; private set; }

        private readonly IViewManager viewManager;

        public EphemerisVM(IViewManager viewManager, List<List<Ephemeris>> ephem, double from, double to, double step, double utcOffset)
        {
            this.viewManager = viewManager;

            SaveToFileCommand = new Command(SaveToFile);
            CloseCommand = new Command(Close);

            var table = new DataTable();
            table.Columns.AddRange(ephem[0].Select(e => new DataColumn() { Caption = e.Key, ColumnName = e.Key }).ToArray());

            for (int i = 0; i < ephem.Count; i++)
            {
                table.Rows.Add(ephem[i].Select(e => e.Formatter.Format(e.Value)).ToArray());
            }

            EphemerisTable = table;
        }

        private void SaveToFile()
        {
            var result = viewManager.ShowSaveFileDialog("Save to file", "Ephemeris", ".csv", "Text files (*.txt)|*.txt|Comma-separated files (*.csv)|*.csv");
            if (result != null)
            {
                
            }
        }
    }
}
