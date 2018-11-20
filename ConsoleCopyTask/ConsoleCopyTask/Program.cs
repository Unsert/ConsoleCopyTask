using System;
using System.IO;
using System.Security;
using System.Threading;

namespace ConsoleCopyTask
{
    class Program
    {
        static void Main()
        {
            /*
                Тестовое задание на должность «Программист-стажер C#»
                Необходимо реализовать консольное приложение, которое в качестве входных данных запрашивает у пользователя следующие обязательные значения:
                1. Путь к исходному каталогу (IN);
                2. Путь к каталогу назначения (OUT);
                3. Интервал чтения данных (I);
                и 0 или несколько необязательных значений:
                1. Количество потоков обработки данных (T, 1<T<10) – по умолчанию чтение происходит в один поток, при задании этого параметра –  в T потоков.
                2. Флаг, задающий, нужно ли удалять прочитанные файлы (D, true/false) – по умолчанию прочитанные файлы не удаляются.
                3. Флаг, задающий, нужно ли в процессе чтения выводить имена прочитанных файлов в консоль (P, true/false) – по умолчанию имена прочитанных файлов не выводятся.
                4. Флаг, задающий, нужно ли читать содержимое вложенных в исходный каталог папок и их содержимое (R, true/false) – по умолчанию вложенные папки и их содержимое игнорируется.
                После ввода данных программа должна начать копировать файлы из каталога IN в каталог OUT с учетом необязательных параметров. 
                Уже скопированные файлы считываться не должны. 
                Эти действия должны повторяться с интервалом I до тех пор, пока пользователь не введет команду stop. 
                После этого программа должна вывести общий объем скопированных данных и затем по нажатию любой клавиши завершиться.
                Необходимо реализовать обработку всех обязательных параметров и хотя бы одного необязательного. 
                При запуске программа должна вывести в консоль список поддерживаемых параметров.
              
                -------------------------------------------------------------------------------------

                Реализованы все обязательные и необязательные параметры.
                Параметр "Интервал чтения данных" исполнен как количество копируемых файлов.
                Параметры флагов вводятся в любом порядке через пробел.
                Для удобства отладки можно вводить букву "a" для перезапуска.
             * 
            */
            char run = 'a';
            while (run.Equals('a')) {
                UserInteraction ui = new UserInteraction();
                FileCopier fc = new FileCopier();
                fc.Copy(ui.GetCopyArgs());
                Console.WriteLine("Program finished. Press \"a\" to run again or any other key to exit.");
                run = Console.ReadKey().KeyChar;
            }
        }
    }


    public struct CopyArgs
    /*
     * Тип данных, хранящий в себе все аргументы для копирования, взятые у пользователя
     */
    {
        public string sourcePath, targetPath;
        public int interval;
        public CopyFlags flags;

        public CopyArgs(string sourcePath, string targetPath, int interval)
        {
            this.sourcePath = sourcePath;
            this.targetPath = targetPath;
            this.interval = interval;
            flags.threads = 1;
            flags.deleteSource = false;
            flags.printCopied = false;
            flags.recursively = false;
        }

        public CopyArgs(string sourcePath, string targetPath, int interval, CopyFlags flags)
        {
            this.sourcePath = sourcePath;
            this.targetPath = targetPath;
            this.interval = interval;
            this.flags = flags;
        }

    }

    public struct CopyFlags
    /*
    * Тип данных, хранящий в себе необязательные флаги копирования, взятые у пользователя
    */
    {
        public int threads;
        public bool deleteSource, printCopied, recursively;

        public CopyFlags(int threads, bool deleteSource, bool printCopied, bool recursively)
        {
            this.threads = threads;
            this.deleteSource = deleteSource;
            this.printCopied = printCopied;
            this.recursively = recursively;
        }
    }

    class FileCopier
    {
        //Общая переменная, хранящая в себе количество скопированных файлов
        public int copycount;
        //Общая переменная для накопления объема скопированных файлов
        public double sizecopied;
        //Переменная для реализации остановки паралелльных потоков в случае, если у пользователя запрашивается продолжение копирования
        public bool isAsking;

        public void Copy(CopyArgs arguments)
        {
            DirectoryInfo diSource = new DirectoryInfo(arguments.sourcePath);
            DirectoryInfo diTarget = new DirectoryInfo(arguments.targetPath);
            copycount = 0;
            sizecopied = 0;
            isAsking = false;
            CopyAll(diSource, diTarget, arguments);
            //Выводит общий объем скопированных данных.
            printCopyInfo(sizecopied, copycount, arguments);
        }

        public void CopyFiles (FileInfo[] fileList, DirectoryInfo source, DirectoryInfo target, CopyArgs arguments)
        {
            foreach(FileInfo fi in fileList)
            {
                CopySingleFile(fi, source, target, arguments);
            }
        }

        public void CopySingleFile(FileInfo fi, DirectoryInfo source, DirectoryInfo target, CopyArgs arguments)        
        //Функция копирует одиночный файл, учитывая все аргументы пользователя.
        {

            //Если точно такой - же файл (по размеру и имени) уже существует в конечной директории - он скопирован не будет
            bool needToCopy = true;
            if (File.Exists(Path.Combine(target.FullName, fi.Name)))
            {
                FileInfo tarfi = new FileInfo(Path.Combine(target.FullName, fi.Name));
                if (fi.Length.Equals(tarfi.Length))
                {
                    needToCopy = false;
                    if (arguments.flags.deleteSource)
                        fi.Delete();
                }
            }
            if (needToCopy)
            {
                string user_stop = "";
                //Тормозит поток, если любой другой поток уже спрашивает что-либо у пользователя.
                while (isAsking)
                    Thread.Sleep(500);
                //Реализация "Интервала копирования файлов"
                if ((copycount % arguments.interval == 0) & (copycount !=0))
                {
                    isAsking = true;
                    Console.WriteLine("Copied {0} files. Type \"stop\" to stop copying or press \"Enter\" to continue", copycount);
                    user_stop = Console.ReadLine();
                    if (user_stop.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        printCopyInfo(sizecopied, copycount, arguments);
                        Console.WriteLine("Program finished. Press any key to exit.");
                        Console.ReadKey();
                        Environment.Exit(1);
                    }
                    isAsking = false;
                }
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), false);
                sizecopied = sizecopied + fi.Length;
                if (arguments.flags.printCopied)
                    Console.WriteLine("Copied file {0}", Path.Combine(source.FullName, fi.Name));
                if (arguments.flags.deleteSource)
                    fi.Delete();
                copycount++;
                
            }
        }

        public void CopyAll(DirectoryInfo source, DirectoryInfo target, CopyArgs arguments)
        {
            Directory.CreateDirectory(target.FullName);
            //Если проставлен флаг рекурсивности - копирует, начиная с самого глубоковложенного файла
            //Если проставлен флаг удаления - удалит директорию, если все файлы скопированы, либо все такие же файлы есть в такой же конечной директории.
            if (arguments.flags.recursively)
            {
                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir, arguments);
                    if (arguments.flags.deleteSource)
                        diSourceSubDir.Delete(true);
                }
            }

            /*
             * Реализация многопоточности.
             * Полный список файлов из директории разбивается на отдельные списки для каждого из потоков
             * Каждый поток проходит только по своему списку.
             */
        
            FileInfo[][] fileInfoLists = SplitFileList(source, arguments.flags.threads);
            int threadscounter = 0;
            Thread[] threads = new Thread[arguments.flags.threads];
            int maxthreads = arguments.flags.threads;
            int threadsused = 0;
            for (threadscounter = 0;  threadscounter < maxthreads; threadscounter++)
            {            
                if (fileInfoLists[threadscounter].Length > 0)
                {
                    //При использовании переменной из цикла и многопоточности индекс может вылететь за границы.
                    //Обходится созданием ещё одной локальной переменной внутри цикла.
                    int localindex = threadscounter;
                    void start()
                    {
                            CopyFiles(fileInfoLists[localindex], source, target, arguments);                         
                    }
                    threads[threadscounter] = new Thread(start);
                    threads[threadscounter].Start();
                    threadsused++;
                }
            }
            //Ждём завершения всех использованных потоков.
            for (int i = 0; i<threadsused; i++)
            {
                int localindex = i;
                threads[localindex].Join();
            }
            
        }
        
        public FileInfo[][] SplitFileList(DirectoryInfo source, int threads)
        /*
         * Разбивает общий список файлов на threads списков.
         * Массив будет с лесенкой, если количество файлов не делится на количество потоков, 
         * но пустых мест в массиве не будет (реализовано через остаток от деления)
         */
        {
            int quotient, remainder, counter;
            FileInfo[] fi = source.GetFiles();
            FileInfo[][] splittedList = new FileInfo[threads][];
            quotient = (fi.Length / threads);
            remainder = (fi.Length % threads);
            for (int i = 0; i<threads; i++)
            {
                counter = (remainder > 0) ? 1 : 0;
                splittedList[i] = new FileInfo[quotient + counter];
                remainder--;
            }
            int tr = 0, tasks = 0; 
            for (int i = 0; i < fi.Length; i++)
            {
                if (tr == threads)
                {
                    tr = 0;
                    tasks++;
                }
                splittedList[tr][tasks] = fi[i];
                tr++;
            }
            return splittedList; 
        }

        

        public void printCopyInfo(double sizecopied, int copycount, CopyArgs arguments)
        //Красиво выводит информацию по скопированным файлам.
        {
            string copystring = convertFileSize(sizecopied);
            Console.WriteLine("Totally copied {0} files, {1}, from {2} to {3}", copycount, copystring, arguments.sourcePath, arguments.targetPath);
        }

        public string convertFileSize(double sizecopied)
        //Конвертирует размер файла в байтах в удобный вид.
        {
            string[] quantificator = { "", "kilo", "mega", "giga", "tera" };
            int divcount = 0;
            while (sizecopied > 1024) {
                sizecopied = sizecopied / 1024;
                divcount++;
            }
            return (Math.Round(sizecopied, 2) + " " + quantificator[divcount]+"bytes");
        }
    }

    class UserInteraction {


        public void PrintCopyArgs(CopyArgs arguments)
        //Выводит список имеющихся аргументов в консоль. Удобна для отладки.
        {
            Console.WriteLine(arguments.sourcePath);
            Console.WriteLine(arguments.targetPath);
            Console.WriteLine(arguments.interval);
            Console.Write("threads = {0}, delete = {1}, print = {2}, recursively = {3} \n",
                arguments.flags.threads, arguments.flags.deleteSource, arguments.flags.printCopied, arguments.flags.recursively);
        }

        public CopyArgs GetCopyArgs()
        {
            /* 
             * Запрашивает все необходимые аргументы у пользователя.
             * Три обязательных параметра будет запрашивать до тех пор, пока не будут даны подходящие ответы
             * (Исходный путь существует, пути - абсолютны (иначе бы файлы копировались в путь программы), интервал - целое число)
             * 4 флага запрашивает 1 раз, при отсутствии любого из них на его место подставит дефолтное значение.
             * Флаги вводятся через пробел в любом порядке.
             */
            CopyArgs args = new CopyArgs(getValidPathFromUser(true), getValidPathFromUser(false), getIntervalValueFromUser(), GetCopyFlagsFromUser());
            return args;
        }

        CopyFlags GetCopyFlagsFromUser()
        {
            Console.WriteLine("Enter optional copy flags separated by white spaces: ");
            Console.WriteLine("    Number of threads between 1 and 10 (default 1);");
            Console.WriteLine("    D to delete copied files in the source folder (default false);");
            Console.WriteLine("    P to print copied files to console (default false);");
            Console.WriteLine("    R to recursively copy from sub-folders (default false).");
            Console.WriteLine("    Example: \"R P 4 D\" - Recursively copy files printing their names in 4 threads with deletion");
            string user_flags = Console.ReadLine();
            string[] arr_flags = user_flags.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int inp_threads, threads = 1;
            //Реализация фичи, что флаги можно вводить все сразу через пробел в любом порядке, для удобства
            bool d = false, p = false, r = false;
            foreach (string s in arr_flags)
            {
                inp_threads = 0;
                if (Int32.TryParse(s, out inp_threads))
                {
                    if ((inp_threads < 1) | (inp_threads > 10))
                        threads = 1;
                    else
                        threads = inp_threads;
                }
                if (s.Equals("D", StringComparison.OrdinalIgnoreCase))
                    d = true;
                if (s.Equals("P", StringComparison.OrdinalIgnoreCase))
                    p = true;
                if (s.Equals("R", StringComparison.OrdinalIgnoreCase))
                    r = true;
            }
            CopyFlags flags = new CopyFlags(threads, d, p, r);
            return flags;
        }

        int getIntervalValueFromUser()
        //Спрашивает интервал копирования у пользователя, пока не будет введено целое число.
        {
            Console.WriteLine("Enter a positive copy interval value");
            string str_interval = "";
            int number = 0;
            while (number <= 0)
            {
                str_interval = Console.ReadLine();
                if (str_interval.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    System.Environment.Exit(1);
                if (!Int32.TryParse(str_interval, out number))
                    Console.WriteLine("Number contains a typo! \nEnter a valid positive number or type \"exit\" to exit the program");  
                if (number <= 0)
                    Console.WriteLine("Number is not positive! \nEnter a valid positive number or type \"exit\" to exit the program");
            }
            return number;
        }

        string getValidPathFromUser(bool isSource)
        /*
         * Спрашивает путь (исходный и конечный) копирования у пользователя, пока не будет введен:
         * Существующий исходный путь
         * Корректный конечный путь (с указанием буквы диска)
         */
        {
            string pathtype = String.Empty;
            if (isSource)
                pathtype = "source";
            else
                pathtype = "target";
            Console.WriteLine("Enter valid {0} path: ", pathtype);
            bool isValid = false;
            string path = String.Empty;
            while (!isValid)
            {
                path = Console.ReadLine();
                if (path.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    System.Environment.Exit(1);
                if (IsValidPath(path, isSource))
                    isValid = true;
                else
                    Console.WriteLine("Path not exists or contains a typo! \nEnter a valid path or type \"exit\" to exit the program");    
            }
            return path;
        }
 
        public bool IsValidPath(string path, bool isSource)
        //Проверка пути (исходного и конечного) на валидность.
        {
            bool status = false;
            if (String.IsNullOrWhiteSpace(path)) { return false; }
            try
            {
                string result = Path.GetFullPath(path);
                status = Path.IsPathRooted(path);
            }
            catch (ArgumentException) { }
            catch (SecurityException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }
            //Если путь - исходный, то проверяет его существование
            if (isSource)
                if (status & Directory.Exists(path))
                    status = true;
                else
                    status = false;
            return status;
        }
    }
}
