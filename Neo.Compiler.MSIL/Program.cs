using Neo.Compiler.MSIL;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Neo.Compiler
{
    public class Program
    {
        //Console.WriteLine("helo ha:"+args[0]); //普通输出
        //Console.WriteLine("<WARN> 这是一个严重的问题。");//警告输出，黄字
        //Console.WriteLine("<WARN|aaaa.cs(1)> 这是ee一个严重的问题。");//警告输出，带文件名行号
        //Console.WriteLine("<ERR> 这是一个严重的问题。");//错误输出，红字
        //Console.WriteLine("<ERR|aaaa.cs> 这是ee一个严重的问题。");//错误输出，带文件名
        //Console.WriteLine("SUCC");//输出这个表示编译成功
        //控制台输出约定了特别的语法
        public static void Main(string[] args)
        {
            //set console
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var log = new DefLogger();
            log.Log("Neo.Compiler.MSIL console app v" + Assembly.GetEntryAssembly().GetName().Version);

            bool bCompatible = false;
            string filename = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    if (args[i] == "--compatible")
                    {
                        bCompatible = true;
                    }

                    //other option
                }
                else
                {
                    filename = args[i];
                }
            }

            if (filename == null)
            {
                log.Log("need one param for DLL filename.");
                log.Log("[--compatible] disable nep8 function and disable SyscallInteropHash");
                log.Log("Example:neon abc.dll --compatible");
                return;
            }
            if (bCompatible)
            {
                log.Log("use --compatible no nep8 and no SyscallInteropHash");
            }
            string onlyname = System.IO.Path.GetFileNameWithoutExtension(filename);
            string filepdb = onlyname + ".pdb";
            var path = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    Directory.SetCurrentDirectory(path);
                }
                catch
                {
                    log.Log("Could not find path: " + path);
                    Environment.Exit(-1);
                }
            }

            ILModule mod = new ILModule(log);
            Stream fs;
            Stream fspdb = null;

            //open file
            try
            {
                fs = System.IO.File.OpenRead(filename);

                if (System.IO.File.Exists(filepdb))
                {
                    fspdb = System.IO.File.OpenRead(filepdb);
                }

            }
            catch (Exception err)
            {
                log.Log("Open File Error:" + err.ToString());
                return;
            }
            //load module
            try
            {
                mod.LoadModule(fs, fspdb);
            }
            catch (Exception err)
            {
                log.Log("LoadModule Error:" + err.ToString());
                return;
            }
            byte[] bytes;
            bool bSucc;
            string abijsonstr = null;
            string debugstr = null;
            // string mdjsonstr = null;
            //convert and build
            try
            {
                var conv = new ModuleConverter(log);
                ConvOption option = new ConvOption
                {
                    useNep8 = !bCompatible,
                    useSysCallInteropHash = !bCompatible
                };
                NeoModule am = conv.Convert(mod, option);
                bytes = am.Build();
                log.Log("convert succ");

                try
                {
                    var outjson = DebugExport.Export(am);
                    StringBuilder sb = new StringBuilder();
                    outjson.ConvertToStringWithFormat(sb, 0);
                    debugstr = sb.ToString();
                    log.Log("gen debug succ");
                }
                catch (Exception err)
                {
                    log.Log("gen debug Error:" + err.ToString());
                }

                // try
                // {
                //     var outjson = MetadataExport.Export(mod);
                //     StringBuilder sb = new StringBuilder();
                //     outjson.ConvertToStringWithFormat(sb, 0);
                //     mdjsonstr = sb.ToString();
                //     log.Log("gen md succ");
                // }
                // catch (Exception err)
                // {
                //     log.Log("gen md Error:" + err.ToString());
                // }

                try
                {
                    var outjson = vmtool.FuncExport.Export(am, bytes);
                    StringBuilder sb = new StringBuilder();
                    outjson.ConvertToStringWithFormat(sb, 0);
                    abijsonstr = sb.ToString();
                    log.Log("gen abi succ");
                }
                catch (Exception err)
                {
                    log.Log("gen abi Error:" + err.ToString());
                }

            }
            catch (Exception err)
            {
                log.Log("Convert Error:" + err.ToString());
                return;
            }
            //write bytes
            try
            {

                string bytesname = onlyname + ".avm";

                System.IO.File.Delete(bytesname);
                System.IO.File.WriteAllBytes(bytesname, bytes);
                log.Log("write:" + bytesname);
                bSucc = true;
            }
            catch (Exception err)
            {
                log.Log("Write Bytes Error:" + err.ToString());
                return;
            }

            try
            {
                string debugname = onlyname + ".debug.json";
                File.Delete(debugname);
                File.WriteAllText(debugname, debugstr);
                log.Log("write:" + debugname);
                bSucc = true;
            }
            catch (Exception err)
            {
                log.Log("Write debug Error:" + err.ToString());
                return;
            }

            // try
            // {
            //     string mdname = onlyname + ".md.json";

            //     System.IO.File.Delete(mdname);
            //     System.IO.File.WriteAllText(mdname, mdjsonstr);
            //     log.Log("write:" + mdname);
            //     bSucc = true;
            // }
            // catch (Exception err)
            // {
            //     log.Log("Write md Error:" + err.ToString());
            //     return;
            // }

            try
            {
                string abiname = onlyname + ".abi.json";

                System.IO.File.Delete(abiname);
                System.IO.File.WriteAllText(abiname, abijsonstr);
                log.Log("write:" + abiname);
                bSucc = true;
            }
            catch (Exception err)
            {
                log.Log("Write abi Error:" + err.ToString());
                return;
            }

            try
            {
                fs.Dispose();
                if (fspdb != null)
                    fspdb.Dispose();
            }
            catch
            {

            }

            if (bSucc)
            {
                log.Log("SUCC");
            }
        }
    }
}
