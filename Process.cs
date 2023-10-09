using Actividad9;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Actividad9
{
    public class Process
    {
        private int id;
        private string name;
        private int size;
        private StatusEnum status;

        public int InitialExecutionTime { get; set; }
        public int RemainingExecutionTime { get; set; }
        public bool IsExecutionComplete { get; internal set; }
        public double Xposition { get; set; }

        public double Yposition { get; set; }
        public int ID
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                }
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                }
            }
        }

        public int Size
        {
            get { return size; }
            set
            {
                if (size != value)
                {
                    size = value;
                }
            }
        }

        public StatusEnum Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                }
            }
        }

        public Process(int id, string name, int size)
        {
            ID = id;
            Size = size;
            Name = name;
            Status = StatusEnum.Waiting;

            Random random = new Random();

            //tiempo de vida mínimo
            int minExecutionTime = 100;
            //tiempo de vida máximo
            int maxExecutionTime = 1000;

            int totalExecutionTime = random.Next(minExecutionTime, maxExecutionTime + 1);

            InitialExecutionTime  = totalExecutionTime;
            RemainingExecutionTime = totalExecutionTime;
        }
    }
}
