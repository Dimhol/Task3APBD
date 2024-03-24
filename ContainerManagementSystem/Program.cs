using System;
using System.Collections.Generic;
using System.Linq;

namespace ContainerManagementSystem
{
    public class OverfillException : Exception
    {
        public OverfillException(string message) : base(message)
        {
        }
    }

    public class ShipOperationException : Exception
    {
        public ShipOperationException(string message) : base(message)
        {
        }
    }

    public interface IHazardNotifier
    {
        void NotifyHazard(string message);
    }

    public abstract class Container
    {
        public double CargoMass { get; protected set; }
        public double Height { get; private set; }
        public double TareWeight { get; private set; }
        public double Depth { get; private set; }
        public string SerialNumber { get; private set; }
        public double MaxPayload { get; private set; }
        
        public ContainerShip CurrentShip { get; set; }

        private static int uniqueNumberCounter = 0;

        protected Container(double height, double tareWeight, double depth, double maxPayload)
        {
            Height = height;
            TareWeight = tareWeight;
            Depth = depth;
            MaxPayload = maxPayload;
            SerialNumber = GenerateSerialNumber();
            CargoMass = 0; 
        }

        private string GenerateSerialNumber()
        {
            return $"KON-C-{++uniqueNumberCounter}";
        }

        public virtual void LoadCargo(double mass)
        {
            if (mass + CargoMass > MaxPayload)
                throw new OverfillException("Loading exceeds maximum payload.");
            CargoMass += mass;
        }

        public virtual void EmptyCargo()
        {
            CargoMass = 0;
        }

        public abstract void CheckForHazards();
    }

    public class LiquidContainer : Container, IHazardNotifier
    {
        public bool IsHazardous { get; private set; }

        public LiquidContainer(double height, double tareWeight, double depth, double maxPayload, bool isHazardous)
            : base(height, tareWeight, depth, maxPayload)
        {
            IsHazardous = isHazardous;
        }

        public override void LoadCargo(double mass)
        {
            double maxAllowed = IsHazardous ? MaxPayload * 0.5 : MaxPayload * 0.9;
            if (mass + CargoMass > maxAllowed)
            {
                NotifyHazard(
                    $"Danger: Overloading a hazardous liquid container. Allowed: {maxAllowed}, Attempted: {mass + CargoMass}");
                throw new OverfillException("Cannot load liquid container beyond its allowed capacity.");
            }

            base.LoadCargo(mass);
        }

        public void NotifyHazard(string message)
        {
            Console.WriteLine($"[Hazard - Liquid Container] {message}");
        }

        public override void CheckForHazards()
        {
            if (IsHazardous && CargoMass > MaxPayload * 0.5)
            {
                NotifyHazard("Hazardous liquid container overfilled!");
            }
        }
    }

    public class GasContainer : Container, IHazardNotifier
    {
        public double Pressure { get; private set; }

        public GasContainer(double height, double tareWeight, double depth, double maxPayload, double pressure)
            : base(height, tareWeight, depth, maxPayload)
        {
            Pressure = pressure;
        }

        public override void LoadCargo(double mass)
        {
            if (mass + CargoMass > MaxPayload)
            {
                NotifyHazard(
                    $"Danger: Overloading a gas container. Allowed: {MaxPayload}, Attempted: {mass + CargoMass}");
                throw new OverfillException("Cannot load gas container beyond its allowed capacity.");
            }

            base.LoadCargo(mass);
        }

        public override void EmptyCargo()
        {
            CargoMass = MaxPayload * 0.05; 
        }

        public void NotifyHazard(string message)
        {
            Console.WriteLine($"[Hazard - Gas Container] {message}");
        }

        public override void CheckForHazards()
        {
            if (CargoMass > MaxPayload)
            {
                NotifyHazard("Gas container pressure hazard!");
            }
        }
    }

    public enum ProductType
    {
        Bananas,
        Chocolate,
        Fish,
        Meat,
        IceCream,
        FrozenPizza,
        Cheese,
        Sausages,
        Butter,
        Eggs
    }

    public class RefrigeratedContainer : Container
    {
        public ProductType StoredProductType { get; private set; }
        public double MaintainedTemperature { get; private set; }

        private readonly Dictionary<ProductType, double> _productTypeTemperatures = new Dictionary<ProductType, double>
        {
            { ProductType.Bananas, 13.3 },
            { ProductType.Chocolate, 18 },
            { ProductType.Fish, 2 },
            { ProductType.Meat, -15 },
            { ProductType.IceCream, -18 },
            { ProductType.FrozenPizza, -30 },
            { ProductType.Cheese, 7.2 },
            { ProductType.Sausages, 5 },
            { ProductType.Butter, 20.5 },
            { ProductType.Eggs, 19 }
        };

        public RefrigeratedContainer(double height, double tareWeight, double depth, double maxPayload,
            ProductType productType)
            : base(height, tareWeight, depth, maxPayload)
        {
            StoredProductType = productType;
            MaintainedTemperature = _productTypeTemperatures[productType];
        }

        public override void CheckForHazards()
        {
            if (MaintainedTemperature > _productTypeTemperatures[StoredProductType])
            {
                Console.WriteLine($"Warning: Temperature for {StoredProductType} is higher than recommended.");
            }
        }
    }

    public class ContainerShip
    {
        private static int uniqueShipIdentifier = 0;
        public string ShipSerialNumber { get; private set; }
        public List<Container> Containers { get; private set; } = new List<Container>();
        public int MaxSpeed { get; private set; }
        public int MaxContainerCount { get; private set; }
        public double MaxWeight { get; private set; }

        public ContainerShip(int maxSpeed, int maxContainerCount, double maxWeight)
        {
            MaxSpeed = maxSpeed;
            MaxContainerCount = maxContainerCount;
            MaxWeight = maxWeight;
            ShipSerialNumber = $"SHIP-{++uniqueShipIdentifier}";
        }

        public void LoadContainer(Container container)
        {
            if (Containers.Count >= MaxContainerCount)
            {
                throw new ShipOperationException("Ship capacity exceeded. Cannot load more containers.");
            }
            if (Containers.Sum(c => c.CargoMass + c.TareWeight) + container.CargoMass + container.TareWeight > MaxWeight)
            {
                throw new ShipOperationException("Weight limit exceeded. Cannot load the container.");
            }

            Containers.Add(container);
            container.CurrentShip = this; 
            Console.WriteLine($"Loaded container {container.SerialNumber} onto {ShipSerialNumber}.");
        }

        public void UnloadContainer(Container container)
        {
            if (!Containers.Remove(container))
            {
                throw new ShipOperationException("Container not found on the ship.");
            }

            container.CurrentShip = null; 
            Console.WriteLine($"Unloaded container {container.SerialNumber} from {ShipSerialNumber}.");
        }
    

        public void ReplaceContainer(string serialNumber, Container newContainer)
        {
            var index = Containers.FindIndex(c => c.SerialNumber == serialNumber);
            if (index != -1)
            {
                Containers[index] = newContainer;
                Console.WriteLine($"Replaced container {serialNumber} with {newContainer.SerialNumber}.");
            }
            else
            {
                throw new ShipOperationException($"No container with serial number {serialNumber} found.");
            }
        }

        public Container GetContainerBySerialNumber(string serialNumber)
        {
            var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
            if (container == null)
                throw new ShipOperationException($"No container with serial number {serialNumber} found.");
            return container;
        }
    }

    class Program
    {
        private static List<ContainerShip> ships = new List<ContainerShip>();
        private static List<Container> containers = new List<Container>();

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                DisplayState();
                DisplayMenu();

                string input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        AddContainerShip();
                        break;
                    case "2":
                        AddContainer();
                        break;
                    case "3":
                        PlaceOrRemoveContainer();
                        break;
                    case "4":
                        Console.WriteLine("Exiting the application.");
                        return;
                    default:
                        Console.WriteLine("Invalid option, please try again.");
                        break;
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey(intercept: true);
            }
        }

        static void DisplayState()
        {
            Console.WriteLine("List of container ships:");
            foreach (var ship in ships)
            {
                Console.WriteLine(
                    $"Ship {ship.ShipSerialNumber} - MaxSpeed: {ship.MaxSpeed}, MaxContainers: {ship.MaxContainerCount}, MaxWeight: {ship.MaxWeight}");
            }

            Console.WriteLine("\nList of containers:");
            foreach (var container in containers)
            {
                string shipAssignment = container.CurrentShip != null
                    ? $" (On Ship: {container.CurrentShip.ShipSerialNumber})"
                    : "";
                Console.WriteLine(
                    $"{container.GetType().Name} {container.SerialNumber} - CargoMass: {container.CargoMass}{shipAssignment}");
            }

            Console.WriteLine();
        }

        static void DisplayMenu()
        {
            Console.WriteLine("Possible actions:");
            Console.WriteLine("1. Add a container ship");
            Console.WriteLine("2. Add a container");
            Console.WriteLine("3. Place/Remove a container on/from a ship");
            Console.WriteLine("4. Exit");
            Console.Write("Select an option: ");
        }

        static void AddContainerShip()
        {
            try
            {
                Console.Write("Enter ship speed (in knots): ");
                int speed = int.Parse(Console.ReadLine());

                Console.Write("Enter max number of containers: ");
                int maxContainers = int.Parse(Console.ReadLine());

                Console.Write("Enter max weight (in tons): ");
                double maxWeight = double.Parse(Console.ReadLine());

                var newShip = new ContainerShip(speed, maxContainers, maxWeight);
                ships.Add(newShip);
                Console.WriteLine($"Container ship added with identifier: {newShip.ShipSerialNumber}.");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid input. Please enter numeric values.");
            }
        }

        static void AddContainer()
        {
            Console.Write("Enter container type (Liquid, Gas, Refrigerated): ");
            string type = Console.ReadLine();
            try
            {
                Container newContainer = null;
                switch (type.ToLower())
                {
                    case "liquid":
                        newContainer = CreateLiquidContainer();
                        break;
                    case "gas":
                        newContainer = CreateGasContainer();
                        break;
                    case "refrigerated":
                        newContainer = CreateRefrigeratedContainer();
                        break;
                    default:
                        Console.WriteLine("Invalid container type.");
                        return;
                }

                if (newContainer != null)
                {
                    double cargoMass = GetDoubleInput("Enter the mass of cargo to load into the container (kg): ");
                    newContainer.LoadCargo(cargoMass); 

                    containers.Add(newContainer);
                    Console.WriteLine("Container added and cargo loaded.");
                }
            }
            catch (OverfillException oe)
            {
                Console.WriteLine($"Error loading cargo: {oe.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void PlaceOrRemoveContainer()
        {
            Console.Write("Enter container serial number: ");
            string serialNumber = Console.ReadLine();
            var container = containers.FirstOrDefault(c => c.SerialNumber == serialNumber);

            if (container == null)
            {
                Console.WriteLine("Container not found.");
                return;
            }

            Console.WriteLine("Do you want to (P)lace or (R)emove the container? [P/R]: ");
            var action = Console.ReadLine().ToUpper();

            switch (action)
            {
                case "P":
                    Console.Write("Enter ship serial number: ");
                    string shipSerialNumber = Console.ReadLine();
                    var ship = ships.FirstOrDefault(s => s.ShipSerialNumber == shipSerialNumber);
                    if (ship != null)
                    {
                        try
                        {
                            ship.LoadContainer(container);
                            Console.WriteLine($"Container {serialNumber} placed on ship {shipSerialNumber}.");
                        }
                        catch (ShipOperationException ex)
                        {
                            Console.WriteLine($"Operation failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ship not found.");
                    }

                    break;
                case "R":
                    if (container.CurrentShip != null)
                    {
                        try
                        {
                            container.CurrentShip.UnloadContainer(container);
                            Console.WriteLine($"Container {serialNumber} removed from its current ship.");
                        }
                        catch (ShipOperationException ex)
                        {
                            Console.WriteLine($"Operation failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The container is not on any ship.");
                    }

                    break;
                default:
                    Console.WriteLine("Invalid action.");
                    break;
            }
        }

        private static LiquidContainer CreateLiquidContainer()
        {
            Console.WriteLine("Creating a new Liquid Container.");
            double height = GetDoubleInput("Enter container height: ");
            double tareWeight = GetDoubleInput("Enter container tare weight: ");
            double depth = GetDoubleInput("Enter container depth: ");
            double maxPayload = GetDoubleInput("Enter container max payload: ");
            Console.Write("Is it hazardous? (yes/no): ");
            bool isHazardous = Console.ReadLine().Trim().ToLower() == "yes";

            return new LiquidContainer(height, tareWeight, depth, maxPayload, isHazardous);
        }

        private static GasContainer CreateGasContainer()
        {
            Console.WriteLine("Creating a new Gas Container.");
            double height = GetDoubleInput("Enter container height: ");
            double tareWeight = GetDoubleInput("Enter container tare weight: ");
            double depth = GetDoubleInput("Enter container depth: ");
            double maxPayload = GetDoubleInput("Enter container max payload: ");
            double pressure = GetDoubleInput("Enter gas pressure: ");

            return new GasContainer(height, tareWeight, depth, maxPayload, pressure);
        }

        private static RefrigeratedContainer CreateRefrigeratedContainer()
        {
            Console.WriteLine("Creating a new Refrigerated Container.");
            double height = GetDoubleInput("Enter container height: ");
            double tareWeight = GetDoubleInput("Enter container tare weight: ");
            double depth = GetDoubleInput("Enter container depth: ");
            double maxPayload = GetDoubleInput("Enter container max payload: ");
            Console.Write("Enter product type (e.g., Bananas, Fish): ");
            ProductType productType = (ProductType)Enum.Parse(typeof(ProductType), Console.ReadLine(), true);

            return new RefrigeratedContainer(height, tareWeight, depth, maxPayload, productType);
        }

        private static double GetDoubleInput(string prompt)
        {
            double value;
            Console.Write(prompt);
            while (!double.TryParse(Console.ReadLine(), out value))
            {
                Console.Write("Invalid input. Please enter a numeric value: ");
            }

            return value;
        }
    }
}
    //naming?