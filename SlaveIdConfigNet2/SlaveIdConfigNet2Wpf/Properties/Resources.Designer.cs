﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace SlaveIdConfigNet2Wpf.Properties {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SlaveIdConfigNet2Wpf.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   使用此强类型资源类，为所有资源查找
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 BMS从机ID配置工具 的本地化字符串。
        /// </summary>
        public static string app_title {
            get {
                return ResourceManager.GetString("app_title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找 System.Drawing.Bitmap 类型的本地化资源。
        /// </summary>
        public static System.Drawing.Bitmap green_energy_symbol {
            get {
                object obj = ResourceManager.GetObject("green_energy_symbol", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   查找类似于 (图标) 的 System.Drawing.Icon 类型的本地化资源。
        /// </summary>
        public static System.Drawing.Icon green_energy_symbol_64X64 {
            get {
                object obj = ResourceManager.GetObject("green_energy_symbol_64X64", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   查找类似 查询从机列表失败 的本地化字符串。
        /// </summary>
        public static string str_bc_fail {
            get {
                return ResourceManager.GetString("str_bc_fail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 分配ID失败 的本地化字符串。
        /// </summary>
        public static string str_first_alloc_fail {
            get {
                return ResourceManager.GetString("str_first_alloc_fail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似  的本地化字符串。
        /// </summary>
        public static string str_ok {
            get {
                return ResourceManager.GetString("str_ok", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 打开CAN失败 的本地化字符串。
        /// </summary>
        public static string str_open_can_fail {
            get {
                return ResourceManager.GetString("str_open_can_fail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 设置ID失败 的本地化字符串。
        /// </summary>
        public static string str_set_all_id_fail {
            get {
                return ResourceManager.GetString("str_set_all_id_fail", resourceCulture);
            }
        }
    }
}
