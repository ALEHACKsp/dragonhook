﻿using DotNetDetour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class Computer
    {
        public string Name { get; set; }
        public Computer() {
            Name = "X1";
        }

        public static string GetCpu()
        {
            return "Intel Core I7";
        }

        public string GetRAMSize()
        {
            return "512M";
        }

        internal string Os
        {
            get
            {
                return "Windows 10";
            }
        }
        public string Work(string msg) {
            return msg + "(" + msg.GetType().Name + " worked)";
        }
        public string Work(int msg) {
            return msg + "(" + msg.GetType().Name + " worked)";
        }
        public int Work(StringBuilder msg) {
            return 123456000+ msg.Length;
        }
        public async Task<string> WorkAsync(string msg) {
            await Task.Delay(1);
            return msg+"(workedAsync)";
        }
        public static string Any<T>(T val) {
            return val.ToString();
        }
        public static async Task<string> AnyAsync<T>(T val) {
            await Task.Delay(1);
            return val.ToString()+"Async";
        }



        private class Item {
            public Computer Value;
            public string Status = "-";
        }
        public string PowerOff() {
            var item = new Item { Value = this };
            return PowerOffList(new List<Item>(new Item[] { item })) + ":" + item.Status;
        }
        private static string PowerOffList(List<Item> pcs) {
            var rtv = "";
            foreach (var pc in pcs) {
                rtv += "," + pc.Value.Name;
                pc.Status = "off";
            }
            return rtv.Substring(1);
        }
        private static string PowerOffList(List<string> pcs) {
            return "干扰测试";
        }
        private static string PowerOffList(object pcs) {
            return "干扰测试";
        }
    }


    public class ComputerOf<T>
    {
        public T ComputerIo(T owner)
        {
            return owner;
        }
        public async Task<T> ComputerIoAsync(T owner) {
            await Task.Delay(1);
            return owner;
        }
    }

    public class ComputerDetour : IMethodHook {
        #region 静态方法HOOK
        [HookMethod("Test.Computer")]
        public static string GetCpu() {
            return "Not " + GetCpu_Original();
        }

        [OriginalMethod]
        public static string GetCpu_Original() {
            Console.WriteLine("GetCpu_Original");
            return null;
        }
        #endregion

        #region 构造方法
        [HookMethod("Test.Computer")]
        public void Computer() {
            Computer_Original();

            //根据上下文来决定是否要hook
            if (CallContext.LogicalGetData("OpenComputerConstructorHook") == (object)"1") {
                var This = (Computer)(object)this;
                This.Name = "ConstructorMethod " + This.Name;
            }
        }

        [OriginalMethod]
        public void Computer_Original() {
            Console.WriteLine("Computer_Original");
        }
        #endregion



        #region 实例方法
        [HookMethod("Test.Computer")]
        public string GetRAMSize() {
            return "Hook " + GetRAMSize_Original();
        }

        [OriginalMethod]
        public string GetRAMSize_Original() {
            Console.WriteLine("GetRAMSize_Original");
            return null;
        }
        #endregion

        #region 实例方法（不实现OriginalMethod、方法名称和原方法名称不同）
        [HookMethod(typeof(Computer), "Work", "WorkFn")]
        public string XXXWork([RememberType("System.String")]object msg) {
            Console.WriteLine("Hook " + WorkFn(msg));
            return "Hook " + WorkFn(msg);
        }
        [HookMethod(typeof(Computer), "Work", "WorkFn")]
        public string XXXWork([RememberType("System.Int32")]int msg) {
            return "Hook " + WorkFn(msg);
        }
        [HookMethod(typeof(Computer), "Work", "WorkFn")]
        public long XXXWork([RememberType("System.Text.StringBuilder")]Encoding msg) {
            return WorkFn(msg)+50;
        }

        [OriginalMethod]
        public object WorkFn(int msg) {//注意此处值类型的参数必须也是完全一样的值类型
            return null;
        }
        [OriginalMethod]
        public long WorkFn(Encoding msg) {//注意此处值类型的返回值必须也是值类型，内存长度必须相同或者更大，否则数据必然丢失
            return 0;
        }
        [OriginalMethod]
        public object WorkFn(object msg) {//和string的签名一致即可正确匹配
            return null;
        }
        [OriginalMethod]
        public object WorkFn(string msg) {//超级干扰，这个方法没有谁用到了
            return null;
        }
        #endregion

        #region 异步实例方法
        [HookMethod(typeof(Computer))]
        public async Task<string> WorkAsync(string msg) {
            return "Hook " + await WorkAsync_Original(msg);
        }
        [OriginalMethod]
        public async Task<string> WorkAsync_Original(string msg) {
            await Task.Delay(1);
            return null;
        }
        #endregion


        #region 实例属性
        //public string get_Os(){...} 不封装成属性也可以
        public string Os {
            [HookMethod(typeof(Computer))]
            get {
                return "Not " + Os_Original;
            }
        }
        //public string get_Os_Original(){...} 不封装成属性也可以
        public string Os_Original {
            [OriginalMethod]
            get {
                return null;
            }
        }
        #endregion






        #region 带私有+内部类型的方法
        [HookMethod("Test.Computer")]
        private static string PowerOffList([RememberType("System.Collections.Generic.List`1[[Test.Computer+Item]]")] object pcs) {
            var msg = PowerOffList_Original(pcs);
            return "InternalTypeMethod " + msg;
        }

        [OriginalMethod]
        private static string PowerOffList_Original(object pcs) {
            return null;
        }
        #endregion







        #region 【不支持】泛型方法<引用类型>，每种使用到的类型都单独实现，如：string，object。引用类型的泛型方法无法正确Hook，原因不明
        [HookMethod("Test.Computer")]
        public static string Any([RememberType(isGeneric: true)]string val) {
            return "Hook<string> " + Any_Original(val);
        }
        [OriginalMethod]
        public static string Any_Original(string val) {
            return null;
        }
        #endregion

        #region 泛型方法<值类型>，每种使用到的类型都单独实现，如：int、bool
        [HookMethod("Test.Computer")]
        public static string Any([RememberType(isGeneric: true)]int val) {
            return "Hook<int> " + Any_Original(val);
        }
        [OriginalMethod]
        public static string Any_Original(int val) {
            return null;
        }
        #endregion

        #region 泛型方法<值类型>，每种使用到的类型都单独实现，如：int、bool
        [HookMethod("Test.Computer")]
        public static async Task<string> AnyAsync([RememberType(isGeneric: true)]int val) {
            return "Hook<int> " + await AnyAsync_Original(val);
        }
        [OriginalMethod]
        public static async Task<string> AnyAsync_Original(int val) {
            await Task.Delay(1);
            return null;
        }
        #endregion





        #region 泛型类型<引用类型>的方法，只能用一个方法进行hook，如：string，object
        [HookMethod(typeof(ComputerOf<Object>))]
        public object ComputerIo(object name) {
            if (name is string) {
                var human = ComputerIo_Original(name);
                human = "Hook<string> " + human;
                return human;
            }
            if (name is Computer) {
                var o = (Computer)ComputerIo_Original(name);
                o.Name = "Hook<object> " + o.Name;
                return o;
            }
            return null;
        }
        [OriginalMethod]
        public object ComputerIo_Original(object owner) {
            return null;
        }
        #endregion

        #region 泛型类型<值类型>的方法，每种使用到的类型都单独实现，如：int、bool
        [HookMethod("Test.ComputerOf`1[[System.Int32]]")]
        public int ComputerIo(int name) {
            return ComputerIo_Original(name) + 1;
        }
        [OriginalMethod]
        public int ComputerIo_Original(int owner) {
            return 0;
        }
        #endregion

        #region 泛型类型<值类型>的方法，每种使用到的类型都单独实现，如：int、bool
        [HookMethod("Test.ComputerOf`1[[System.Int32]]")]
        public async Task<int> ComputerIoAsync(int name) {
            return await ComputerIoAsync_Original(name) + 1;
        }
        [OriginalMethod]
        public async Task<int> ComputerIoAsync_Original(int owner) {
            await Task.Delay(1);
            return 0;
        }
        #endregion







        private class Framework : IMethodHook {
            [HookMethod("System.IO.File")]
            public static string ReadAllText(string file) {
                return "Hook " + ori(file) + "：NetFrameworkDetour";
            }

            //一个hook的OriginalMethod名字可以随便写
            [OriginalMethod]
            public static string ori(string file) {
                return null;
            }
        }
    }
    public class Program
    {
        public static void Main1(string[] args)
        {
            MethodHook.Install();
            test();
            Console.WriteLine("end");
            //Console.ReadLine();
            //Console.ReadLine();
            //Console.ReadLine();
            //Console.ReadLine();

        }
        public static void test()
        {
            
            Console.WriteLine("test .net hook");
            Computer com = new Computer();
            Console.WriteLine(com.GetRAMSize());
            Console.WriteLine(Computer.GetCpu());

            Console.WriteLine(com.GetRAMSize());
            Console.WriteLine(Computer.GetCpu());
        }

    }

    namespace Solid
    {
        //假设有一个已存在的类（并且无法修改源码，如.Net框架的方法）
        public class SolidClass
        {
            public string Run(string msg)
            {
                return msg + "(run)";
            }
        }
    }

    namespace Test
    {
        //我们自行实现一个类来修改Run方法的行为，此类用IMethodHook接口修饰
        public class MyClass : IMethodHook
        {
            //我们实现一个新Run方法，并标记为HookMethod，覆盖SolidClass中的Run方法
            [HookMethod("Test.Solid.SolidClass")]
            public string Run(string msg)
            {
                return "Hook " + Run_Original(msg);
            }

            //实现一个占位方法，此方法代表被Hook覆盖的原始方法
            [OriginalMethod]
            public string Run_Original(string msg)
            {
                return null; //这里写什么无所谓，能编译过即可
            }
            public static void Main(string[] args)
            {
                MethodHook.Install();
                test();
            }

            private static void test()
            {
                Console.WriteLine("test .net hook");
                var msg = new Solid.SolidClass();
                Console.WriteLine(msg.Run("Hello World!"));
                Console.WriteLine(msg.Run("Hello World!"));
                Console.WriteLine("test .net hook end");
            }
        }
        
    }

}
