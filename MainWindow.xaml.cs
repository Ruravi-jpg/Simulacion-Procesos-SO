using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Actividad9
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Process> processList = new();
        private ConcurrentQueue<Process> newProcessesQueue = new();

        List<MemoryBlock> realMemory;
        List<MemoryPage> virtualMemory;

        static int IDCounter = 0;
        private bool simulationRunning = false;
        private readonly int simulationSpeed = 800;
        private double yOffset = 10;
        private double lastXPosition = 0;

        int timeQuantum;
        int realMemorySize;
        int virtualMemorySize;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            ProcessListToAddGrid.ItemsSource = processList;

            if (!Int32.TryParse(TimeQuantumValue.Text, out timeQuantum))
            {
                timeQuantum = 100;
            }

            if (!Int32.TryParse(realMemorySizeValue.Text, out realMemorySize))
            {
                realMemorySize = 200;
            }

            if (!Int32.TryParse(virtualMemorySizeValue.Text, out virtualMemorySize))
            {
                virtualMemorySize = 500;
            }

            realMemory = Enumerable.Range(0, realMemorySize)
            .Select(i => new MemoryBlock(i, 1))
            .ToList();
           virtualMemory = Enumerable.Range(0, virtualMemorySize)
            .Select(i => new MemoryPage(i, 1))
            .ToList();


            for (int i = 0; i < realMemorySize; i++)
            {
                
                RealMemoryPanel.Children.Add(new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Colors.Black),
                    Fill = new SolidColorBrush(Colors.Gray),
                });
                
            }

            for (int i = 0; i < virtualMemorySize; i++)
            {
                VirtualMemoryPanel.Children.Add(new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Colors.Black),
                    Fill = new SolidColorBrush(Colors.Gray),
                });
            }
        }
        private void MainProcessGridView_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            List<string> propertiesToIgnore = new() { "IsExecutionComplete", "Xposition", "Yposition" };

            if (propertiesToIgnore.Contains(e.PropertyName))
            {
                e.Cancel = true;
            }
        }

        private async Task StartSimulationAsync()
        {
            simulationRunning = true;

            await Task.Run(async () =>
            {
                while (simulationRunning)
                {
                    int index = 0;

                    // Iterar la lista de procesos
                    while (index < processList.Count)
                    {
                        var currentProcess = processList[index];

                        
                        if (currentProcess.Status == StatusEnum.Waiting)
                        {
                            // Intentamos alocar el proceso en la memorira real
                            if (AllocateMemoryBestFit(realMemory, currentProcess))
                            {
                                currentProcess.Status = StatusEnum.RunningInRealMemory;
                            }
                            //si no podemos alocar en la memoria real lo intentamos en la virtual
                            else if (AllocateMemoryBestFit(virtualMemory, currentProcess))
                            {
                                currentProcess.Status = StatusEnum.RunningInVirtualMemory;
                            }
                            else
                            {
                                index++;
                                continue;
                            }
                        }
                        else if (currentProcess.Status == StatusEnum.Finished)
                        {
                            // si el actual proceso ya está terminado, pasamos al siguiente.
                            index++;
                            continue;
                        }

                        //logica de ejecución del proceso*******************************
                        if (currentProcess.Status == StatusEnum.RunningInRealMemory || currentProcess.Status == StatusEnum.RunningInVirtualMemory)
                        {
                            // le restamos al tiempo de vida restante ya sea un quantum de tiempo o lo que le quede si es menos que un quantum de tiempo
                            int executionTime = Math.Min(timeQuantum, currentProcess.RemainingExecutionTime);
                            currentProcess.RemainingExecutionTime -= executionTime;

                            if (currentProcess.RemainingExecutionTime <= 0)
                            {
                                // en caso de que el proceso ya no tenga tiempo de vida se tiene que desalocar su espacio de memoria, dependiendo de en donde se estaba ejecutando
                                if (currentProcess.Status == StatusEnum.RunningInRealMemory)
                                    DeallocateMemory(realMemory, currentProcess.ID);
                                else if (currentProcess.Status == StatusEnum.RunningInVirtualMemory)
                                    DeallocateMemory(virtualMemory, currentProcess.ID);

                                currentProcess.Status = StatusEnum.Finished;
                            }
                        }
                        index++;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateProcessBarGraph();
                            ProcessListToAddGrid.Items.Refresh();
                        });

                        await Task.Delay(simulationSpeed);
                    }
                }
            });
        }

        private void AddNewProcess(Process newProcess)
        {
            newProcessesQueue.Enqueue(newProcess);
            processList.Add(newProcess);
            UpdateProcessBarGraph();
            ProcessListToAddGrid.Items.Refresh();
        }


        private void UpdateProcessBarGraph()
        {

            MemoryCanvas.Children.Clear(); 

            double barHeight = 13;

            yOffset = 10;
            lastXPosition = 0;

            foreach (var process in processList)
            {
                double barWidth = process.InitialExecutionTime / 10;
                double xPosition = CalculateXPosition(process, barWidth);

                yOffset += barHeight + 5;

                process.Xposition = xPosition;
                process.Yposition = yOffset;

             
                double completionPercentage = (double)(process.InitialExecutionTime - process.RemainingExecutionTime) / process.InitialExecutionTime;

               
                double greenWidth = barWidth * completionPercentage;

                Rectangle redRectangle = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = Brushes.Yellow,
                };

              
                Rectangle greenRectangle = new Rectangle
                {
                    Width = greenWidth,
                    Height = barHeight,
                    Fill = Brushes.Green
                };

                // Position the rectangles on the canvas
                Canvas.SetLeft(redRectangle, xPosition);
                Canvas.SetTop(redRectangle, process.Yposition);

                Canvas.SetLeft(greenRectangle, xPosition); 
                Canvas.SetTop(greenRectangle, process.Yposition);

                MemoryCanvas.Children.Add(redRectangle);
                MemoryCanvas.Children.Add(greenRectangle);
            }

          

        }

        private double CalculateXPosition(Process process, double barWidth)
        {
            double xPosition;

            if (lastXPosition == 0)
            {
 
                xPosition = 0;
            }
            else
            {

                xPosition = lastXPosition;
            }

            lastXPosition = xPosition + barWidth; 
            return xPosition;
        }


        private void DeallocateMemory<T>(List<T> memoryBlocks, int processID) where T : IMemoryItem
        {

            // iteramos en la memoria hasta encontrar un bloque de memoria que pertenezca al proceso
            for (int i = 0; i < memoryBlocks.Count; i++)
            {
                if (memoryBlocks[i].ProcessID == processID)
                {
                    //actualizar la vista visual de la memoria
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                            Rectangle myRec;

                            if (memoryBlocks.GetType() == typeof(List<MemoryBlock>))
                            {
                                myRec = (Rectangle)RealMemoryPanel.Children[i];
                                myRec.Fill = new SolidColorBrush(Colors.Gray);
                            }
                            else if (memoryBlocks.GetType() == typeof(List<MemoryPage>))
                            {
                                myRec = (Rectangle)VirtualMemoryPanel.Children[i];
                                myRec.Fill = new SolidColorBrush(Colors.Gray);
                            }
                    });

                    //marcamos como libre ese bloque
                    memoryBlocks[i].ProcessID = -1;
                }
            }
        }

        private void AddNewProcessButton_Click(object sender, RoutedEventArgs e)
        {

            if (NewTaskName.Text == "")
                return;
            if (NewTaskSize.Text == "" || !Int32.TryParse(NewTaskSize.Text, out int Size))
                return;

            Process newProcess = new(IDCounter, NewTaskName.Text, Size);

            AddNewProcess(newProcess);

            IDCounter++;
            UpdateProcessBarGraph();

            ProcessListToAddGrid.Items.Refresh();
        }

        private bool AllocateMemoryBestFit<T>(List<T> memoryBlocks, Process process) where T : IMemoryItem
        {
            int bestFitStartIndex = -1;
            int currentBlockSize = 0;

            // iteramos en la memoria hasta encontrar un bloque de memoria que pertenezca al proceso
            for (int i = 0; i < memoryBlocks.Count; i++)
            {
                if (memoryBlocks[i].ProcessID == -1)
                {
                    currentBlockSize++;

                    if (currentBlockSize >= process.Size)
                    {
                        // Update the best fit block information
                        bestFitStartIndex = i - currentBlockSize + 1;
                        break;
                    }
                }
                else
                {
                    currentBlockSize = 0;
                }
            }

            if (bestFitStartIndex != -1)
            {
                // actualizamos la vista gráfica de la memoria
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //iteramos a partir del índice del bestFit
                    for (int i = bestFitStartIndex; i < bestFitStartIndex + process.Size; i++)
                    {
                        memoryBlocks[i].ProcessID = process.ID;
                        Rectangle myRec;

                        if (memoryBlocks.GetType() == typeof(List<MemoryBlock>))
                        {
                            // Update the color of real memory UI rectangles to green
                            myRec = (Rectangle)RealMemoryPanel.Children[i];
                            myRec.Fill = new SolidColorBrush(Colors.Green);
                        }
                        else if (memoryBlocks.GetType() == typeof(List<MemoryPage>))
                        {
                            // Update the color of virtual memory UI rectangles to blue
                            myRec = (Rectangle)VirtualMemoryPanel.Children[i];
                            myRec.Fill = new SolidColorBrush(Colors.Blue);
                        }
                    }
                });
                return true;
            }

            return false;
        }



        private async void StartSmiulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (processList.Count == 0)
            {
                MessageBox.Show("No Hay procesos");
                return;
            }

            await StartSimulationAsync();
        }

        private void UpdateVariablesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Int32.TryParse(TimeQuantumValue.Text, out timeQuantum))
            {
                timeQuantum = 100;
            }

            if (!Int32.TryParse(realMemorySizeValue.Text, out realMemorySize))
            {
                realMemorySize = 200;
            }

            if (!Int32.TryParse(virtualMemorySizeValue.Text, out virtualMemorySize))
            {
                virtualMemorySize = 500;
            }


            realMemory = Enumerable.Range(0, realMemorySize)
            .Select(i => new MemoryBlock(i, 1))
            .ToList();
            virtualMemory = Enumerable.Range(0, virtualMemorySize)
             .Select(i => new MemoryPage(i, 1))
             .ToList();

            RealMemoryPanel.Children.Clear();
            for (int i = 0; i < realMemorySize; i++)
            {

                RealMemoryPanel.Children.Add(new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Colors.Black),
                    Fill = new SolidColorBrush(Colors.Gray),
                });

            }

            VirtualMemoryPanel.Children.Clear();
            for (int i = 0; i < virtualMemorySize; i++)
            {
                VirtualMemoryPanel.Children.Add(new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Colors.Black),
                    Fill = new SolidColorBrush(Colors.Gray),
                });
            }
        }
    }
}