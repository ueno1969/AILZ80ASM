﻿using AILZ80ASM.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AILZ80ASM.Assembler
{
    public static class AsmCommandLine
    {
        public static RootCommand SettingRootCommand()
        {
            var rootCommand = new RootCommand(
             description: "AILight Z80 Assember.");

            rootCommand.AddOption(new Option<FileInfo[]>()
            {
                Name = "input",
                ArgumentName = "files",
                Aliases = new[] { "-i", "--input" },
                Description = "アセンブリ対象のファイルをスペース区切りで指定します。",
                Required = true,
                IsSimple = true,
                IsDefineOptional = true,
            });

            // 隠しコマンド（将来の拡張用）
            rootCommand.AddOption(new Option<string>()
            {
                Name = "inputMode",
                ArgumentName = "mode",
                Aliases = new[] { "-im", "--input-mode" },
                Description = "入力ファイルのモードを選択します。",
                DefaultValue = "txt",
                Parameters = new[] { new Parameter { Name = "txt", Description = "テキストファイルを入力します。" } },
                Required = false,
                IsHide = true,
            });

            rootCommand.AddOption(new Option<string>()
            {
                Name = "inputEncode",
                ArgumentName = "mode",
                Aliases = new[] { "-ie", "--input-encode" },
                Description = "入力ファイルのエンコードを選択します。",
                DefaultValue = "auto",
                Parameters = new[] { new Parameter { Name = "auto", Description = "自動判断します。不明な場合はUTF-8で処理します。" },
                                     new Parameter { Name = "utf-8", Description = "入力ファイルをUTF-8で開きます。" },
                                     new Parameter { Name = "shift_jis", Description = "入力ファイルをSHIFT_JISで開きます" },},
                Required = false
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "output",
                ArgumentName = "file",
                Aliases = new string[] { "-o", "--output" },
                Description = "出力ファイルを指定します。",
                Required = true,
                DefaultFunc = (options) => { return GetDefaulFilenameForOutput(options); }
            });

            rootCommand.AddOption(new Option<string>()
            {
                Name = "outputMode",
                ArgumentName = "mode",
                Aliases = new[] { "-om", "--output-mode" },
                Description = "出力ファイルのモードを選択します。",
                DefaultValue = "bin",
                Parameters = new[] { 
                                        new Parameter { Name = "bin", ShortCut = "-bin", Description = "出力ファイルをBIN形式で出力します。" },
                                        new Parameter { Name = "hex", ShortCut = "-hex", Description = "出力ファイルをHEX形式で出力します。" },
                                        new Parameter { Name = "t88", ShortCut = "-t88", Description = "出力ファイルをT88形式で出力します。" },
                                        new Parameter { Name = "cmt", ShortCut = "-cmt", Description = "出力ファイルをCMT形式で出力します。" },
                                        new Parameter { Name = "mzt", ShortCut = "-mzt", Description = "出力ファイルをMZT形式で出力します。" },
                                        new Parameter { Name = "sym", ShortCut = "-sym", Description = "シンボルファイルを出力します。" },
                                        new Parameter { Name = "equ", ShortCut = "-equ", Description = "イコールラベルファイルを出力します。" },
                                        new Parameter { Name = "lst", ShortCut = "-lst", Description = "リストファイルを出力します。" },
                                        new Parameter { Name = "err", ShortCut = "-err", Description = "エラーファイルを出力します。" },
                                        //new Parameter { Name = "dbg", ShortCut = "-dbg", Description = "デバッグファイルを出力します。" },
                                        new Parameter { Name = "tag", ShortCut = "-tag", Description = "タグファイルを出力します。" },
                                    },
                Required = false
            });

            rootCommand.AddOption(new Option<string>()
            {
                Name = "outputEncode",
                ArgumentName = "mode",
                Aliases = new[] { "-oe", "--output-encode" },
                Description = "出力ファイルのエンコードを選択します。",
                DefaultValue = "auto",
                Parameters = new[] { new Parameter { Name = "auto", Description = "自動判断します。不明な場合はUTF-8で処理します。" },
                                     new Parameter { Name = "utf-8", Description = "入力ファイルをUTF-8で開きます。" },
                                     new Parameter { Name = "shift_jis", Description = "入力ファイルをSHIFT_JISで開きます" },},
                Required = false
            });

            // 隠しコマンド
            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputBin",
                ArgumentName = "file",
                Aliases = new[] { "-bin" },
                Description = "BIN形式で出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".bin"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputHex",
                ArgumentName = "file",
                Aliases = new[] { "-hex" },
                Description = "HEX形式で出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".hex"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputT88",
                ArgumentName = "file",
                Aliases = new[] { "-t88" },
                Description = "T88形式で出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".t88"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputCMT",
                ArgumentName = "file",
                Aliases = new[] { "-cmt" },
                Description = "CMT形式で出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".cmt"); }
            });


            rootCommand.AddOption(new Option<FileInfo>() {
                Name = "outputMZT",
                ArgumentName = "file",
                Aliases = new[] { "-mzt" },
                Description = "MZT形式で出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".mzt"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputSYM",
                ArgumentName = "file",
                Aliases = new[] { "-sym" },
                Description = "シンボルファイルを出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".sym"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputEQU",
                ArgumentName = "file",
                Aliases = new[] { "-equ" },
                Description = "イコールラベルファイルを出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".equ"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputADR",
                ArgumentName = "file",
                Aliases = new[] { "-adr" },
                Description = "アドレスラベルファイルを出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".adr"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputLST",
                ArgumentName = "file",
                Aliases = new[] { "-lst" },
                Description = "リストファイルを出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".lst"); }
            });

            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputERR",
                ArgumentName = "file",
                Aliases = new[] { "-err" },
                Description = "アセンブル結果を出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".err"); }
            });


            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "outputTAG",
                ArgumentName = "file",
                Aliases = new[] { "-tag" },
                Description = "タグファイルを出力します。（file名は省略可能）",
                Required = false,
                IsShortCut = true,
                IsSimple = true,
                DefaultFunc = (options) => { return new[] { "tags" }; }
            });


            /*
            rootCommand.AddOption(new Option<FileInfo>()
            {
                Name = "debug",
                Aliases = new[] { "-d", "--debug" },
                Description = "デバッグ情報を記録します",
                Required = false,
                IsHide = true,
                DefaultFunc = (options) => { return GetDefaulFilename(options, ".dbg"); }
            });
            */

            rootCommand.AddOption(new Option<string>()
            {
                Name = "listMode",
                ArgumentName = "mode",
                Aliases = new[] { "-lm", "--list-mode" },
                Description = "リストの出力形式を選択します。",
                DefaultValue = "full",
                Parameters = new[] { new Parameter { Name = "simple", Description = "最小の項目で出力します。" },
                                     new Parameter { Name = "middle", Description = "出力アドレス無しで出力します。" },
                                     new Parameter { Name = "full", Description = "出力アドレスを含めて出力します。" },},
                Required = false
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "listOmitBinaryFile",
                Aliases = new[] { "-lob", "--list-omit-binary-file" },
                Description = "リストの出力でバイナリーファイルを省略出力をします。",
                Required = false
            });

            rootCommand.AddOption(new Option<string>()
            {
                Name = "symbolMode",
                ArgumentName = "mode",
                Aliases = new[] { "-sm", "--symbol-mode" },
                Description = "シンボルの出力形式を選択します。",
                DefaultValue = "normal",
                Parameters = new[] { new Parameter { Name = "minimal-equ", Description = "最小の項目で出力します。" },
                                     new Parameter { Name = "normal", Description = "通常モードで出力します。" },},
                Required = false
            });

            rootCommand.AddOption(new Option<string[]>()
            {
                Name = "omitHeader",
                ArgumentName = "types",
                Aliases = new[] { "-oh", "--omit-header" },
                Description = "ヘッダーを省略します。複数指定可。",
                Parameters = new[]
                {
                    new Parameter { Name = "sym", Description = "シンボルファイル (*.sym)" },
                    new Parameter { Name = "lst", Description = "リストファイル (*.lst)" },
                    new Parameter { Name = "equ", Description = "イコールラベルファイル (*.equ)" },
                    new Parameter { Name = "adr", Description = "アドレスラベルファイル (*.adr)" },
                    new Parameter { Name = "tag", Description = "タグファイル (*.tag)" },
                },
                Required = false
            });

            rootCommand.AddOption(new Option<ushort?>()
            {
                Name = "entryPoint",
                ArgumentName = "address",
                Aliases = new[] { "-ep", "--entry-point" },
                Description = "エントリーポイントを指定します。",
                Required = false
            });

            rootCommand.AddOption(new Option<ushort?>()
            {
                Name = "loadAddress",
                ArgumentName = "address",
                Aliases = new[] { "-la", "--load-address" },
                Description = "ロードアドレスを指定します。（MZTで利用）",
                Required = false
            });

            rootCommand.AddOption(new Option<int>()
            {
                Name = "tabSize",
                ArgumentName = "size",
                Aliases = new[] { "-ts", "--tab-size" },
                Description = "TABのサイズを指定します。",
                DefaultValue = "4",
                Required = false
            });

            rootCommand.AddOption(new Option<Error.ErrorCodeEnum[]>()
            {
                Name = "disableWarningCode",
                ArgumentName = "codes",
                Aliases = new[] { "-dw", "--disable-warning" },
                Description = "Warning、Informationをオフにするコードをスペース区切りで指定します。",
                Required = false,
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "unUsedLabel",
                Aliases = new[] { "-ul", "--unused-label" },
                Description = "未使用ラベルを確認します。",
                Required = false,
            }); ;

            rootCommand.AddOption(new Option<DirectoryInfo>()
            {
                Name = "changeDirectory",
                ArgumentName = "directory",
                Aliases = new[] { "-cd", "--change-dir" },
                Description = "アセンブル実行時のカレントディレクトリを変更します。終了時に元に戻ります。",
                Required = false,
            });

            rootCommand.AddOption(new Option<byte>()
            {
                Name = "gapByte",
                Aliases = new[] { "-gap", "--gap-default" },
                Description = "アセンブラのギャップのデフォルト値を指定します。",
                DefaultValue = "$FF",
                Required = false,
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "diffFile",
                Aliases = new[] { "-df", "--diff-file" },
                Description = "アセンブル出力結果のDIFFを取ります。アセンブル結果は出力されません。",
                Required = false,
            });

            rootCommand.AddOption(new Option<string[]>()
            {
                Name = "defineLabel",
                ArgumentName = "labels",
                Aliases = new[] { "-dl", "--define-label" },
                Description = "ラベルをスペース区切りで指定します。値を設定するときは = で代入します。",
                Required = false
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "noSuperAssemble",
                Aliases = new[] { "-nsa", "--no-super-asm" },
                Description = "スーパーアセンブルモードを無効にします。",
                Required = false,
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "force",
                Aliases = new[] { "-f", "--force" },
                Description = "出力ファイルを上書きします。",
                Required = false,
                IsSimple = true,
            });

            rootCommand.AddOption(new Option<ushort?>()
            {
                Name = "startAddress",
                ArgumentName = "address",
                Aliases = new[] { "-sa", "--start-address" },
                Description = "スタートアドレス(出力)を指定します。",
                Required = false
            });

            rootCommand.AddOption(new Option<DirectoryInfo[]>()
            {
                Name = "includePaths",
                ArgumentName = "paths",
                Aliases = new[] { "-ips", "--include-paths" },
                Description = "インクルードするファイルの検索パスを指定します。",
                Required = false
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "compatRawString",
                Aliases = new[] { "-crs", "--compat-raw-string" },
                Description = "すべての文字列をエスケープ無効の互換モード（@付き文字列）として扱います。",
                Required = false,
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "version",
                Aliases = new[] { "-v", "--version" },
                Description = "バージョンを表示します。",
                Required = false,
                OptionFunc = (argument) => { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
            });

            rootCommand.AddOption(new Option<string>()
            {
                Name = "help",
                Aliases = new[] { "-?", "-h", "--help" },
                Description = "ヘルプを表示します。各オプションの詳細ヘルプを表示します。例： -h --input-mode",
                Required = false,
                IsHelp = true,
                IsSimple = true,
                OptionFunc = (argument) => { return rootCommand.HelpCommand(argument); }
            });

            rootCommand.AddOption(new Option<bool>()
            {
                Name = "readme",
                Aliases = new[] { "-??", "--readme" },
                Description = "Readme.mdを表示します。",
                Required = false,
                IsHelp = true,
                IsSimple = true,
                OptionFunc = (argument) => ReadMe(),
            });

            return rootCommand;
        }

        public static Dictionary<AsmEnum.FileTypeEnum, FileInfo[]> GetInputFiles(this RootCommand rootCommand)
        {
            var result = new Dictionary<AsmEnum.FileTypeEnum, FileInfo[]>();

            result.Add(AsmEnum.FileTypeEnum.Z80, rootCommand.GetValue<FileInfo[]>("input"));

            return result;
        }

        public static Dictionary<AsmEnum.FileTypeEnum, FileInfo> GetOutputFiles(this RootCommand rootCommand)
        {
            var result = new Dictionary<AsmEnum.FileTypeEnum, FileInfo>();

            var output = rootCommand.GetValue<FileInfo>("output");
            var outputSelected = rootCommand.GetSelected("output");
            var outputMode = rootCommand.GetValue<string>("outputMode");
            var outputModeSelected = rootCommand.GetSelected("outputMode");
            
            var outputDic = new Dictionary<AsmEnum.FileTypeEnum, string>
            {
                [AsmEnum.FileTypeEnum.BIN] = "outputBin",
                [AsmEnum.FileTypeEnum.HEX] = "outputHex",
                [AsmEnum.FileTypeEnum.T88] = "outputT88",
                [AsmEnum.FileTypeEnum.CMT] = "outputCMT",
                [AsmEnum.FileTypeEnum.MZT] = "outputMZT",
                [AsmEnum.FileTypeEnum.SYM] = "outputSYM",
                [AsmEnum.FileTypeEnum.EQU] = "outputEQU",
                [AsmEnum.FileTypeEnum.ADR] = "outputADR",
                [AsmEnum.FileTypeEnum.LST] = "outputLST",
                [AsmEnum.FileTypeEnum.ERR] = "outputERR",
                //[AsmEnum.FileTypeEnum.DBG] = "outputDBG",
                [AsmEnum.FileTypeEnum.TAG] = "outputTAG",
            };

            foreach (var item in outputDic)
            {
                var outputFileInfo = rootCommand.GetValue<FileInfo>(item.Value);
                if (outputFileInfo != default)
                {
                    result.Add(item.Key, outputFileInfo);
                }

            }

            if (result.Count == 0 || outputSelected || outputModeSelected)
            {
                var outputModeEnum = outputMode switch
                {
                    "bin" => AsmEnum.FileTypeEnum.BIN,
                    "hex" => AsmEnum.FileTypeEnum.HEX,
                    "t88" => AsmEnum.FileTypeEnum.T88,
                    "cmt" => AsmEnum.FileTypeEnum.CMT,
                    "mzt" => AsmEnum.FileTypeEnum.MZT,
                    "lst" => AsmEnum.FileTypeEnum.LST,
                    "sym" => AsmEnum.FileTypeEnum.SYM,
                    "equ" => AsmEnum.FileTypeEnum.EQU,
                    "adr" => AsmEnum.FileTypeEnum.ADR,
                    "dbg" => AsmEnum.FileTypeEnum.DBG,
                    "err" => AsmEnum.FileTypeEnum.ERR,
                    "tag" => AsmEnum.FileTypeEnum.TAG,
                    _ => throw new InvalidOperationException()
                };
                result.Add(outputModeEnum, output);
            }

            return result;
        }

        public static AsmEnum.EncodeModeEnum GetInputEncodeMode(this RootCommand rootCommand)
        {
            var outputMode = rootCommand.GetValue<string>("inputEncode");

            return GetEncodeMode(outputMode);
        }

        public static AsmEnum.EncodeModeEnum GetOutputEncodeMode(this RootCommand rootCommand)
        {
            var outputMode = rootCommand.GetValue<string>("outputEncode");

            return GetEncodeMode(outputMode);
        }

        private static AsmEnum.EncodeModeEnum GetEncodeMode(string target)
        {
            var encodeMode = target switch
            {
                "auto" => AsmEnum.EncodeModeEnum.AUTO,
                "utf-8" => AsmEnum.EncodeModeEnum.UTF_8,
                "shift_jis" => AsmEnum.EncodeModeEnum.SHIFT_JIS,
                _ => throw new InvalidOperationException()
            };

            return encodeMode;
        }

        public static AsmEnum.ListFormatEnum GetListMode(this RootCommand rootCommand)
        {
            var listMode = rootCommand.GetValue<string>("listMode");

            var mode = listMode switch
            {
                "simple" => AsmEnum.ListFormatEnum.Simple,
                "middle" => AsmEnum.ListFormatEnum.Middle,
                "full" => AsmEnum.ListFormatEnum.Full,
                _ => throw new InvalidOperationException()
            };

            return mode;
        }

        public static AsmEnum.SymbolFormatEnum GetSymbolMode(this RootCommand rootCommand)
        {
            var symbolMode = rootCommand.GetValue<string>("symbolMode");

            var mode = symbolMode switch
            {
                "minimal-equ" => AsmEnum.SymbolFormatEnum.Minimal_Equ,
                "normal" => AsmEnum.SymbolFormatEnum.Normal,
                _ => throw new InvalidOperationException()
            };

            return mode;
        }

        public static int GetTabSize(this RootCommand rootCommand)
        {
            var tabSize = rootCommand.GetValue<int>("tabSize");

            return tabSize;
        }

        public static AsmEnum.FileTypeEnum[] GetOmitHeaders(this RootCommand rootCommand)
        {
            var result = new List<AsmEnum.FileTypeEnum>();
            var omitHeaders = rootCommand.GetValue<string[]>("omitHeader");

            if (omitHeaders != default)
            {
                foreach (var omitHeader in omitHeaders)
                {
                    var fileType = omitHeader switch
                    {
                        "lst" => AsmEnum.FileTypeEnum.LST,
                        "sym" => AsmEnum.FileTypeEnum.SYM,
                        "equ" => AsmEnum.FileTypeEnum.EQU,
                        "adr" => AsmEnum.FileTypeEnum.ADR,
                        "dbg" => AsmEnum.FileTypeEnum.DBG,
                        "err" => AsmEnum.FileTypeEnum.ERR,
                        "tag" => AsmEnum.FileTypeEnum.TAG,
                        _ => throw new InvalidOperationException()
                    };
                    result.Add(fileType);
                }
            }

            return result.ToArray();
        }

        private static string[] GetDefaulFilenameForOutput(IOption[] options)
        {
            var inputOption = (Option<FileInfo[]>)options.Where(m => m.Name == "input").FirstOrDefault();
            var outputOption = (Option<FileInfo>)options.Where(m => m.Name == "output").FirstOrDefault();
            var outputModeOption = (Option<string>)options.Where(m => m.Name == "outputMode").FirstOrDefault();

            if (inputOption == default || outputOption == default || outputModeOption == default)
            {
                return Array.Empty<string>();
            }

            if (inputOption.Value == default || inputOption.Value.Length == 0)
            {
                return Array.Empty<string>();
            }

            if (string.IsNullOrEmpty(outputModeOption.Value))
            {
                return Array.Empty<string>();
            }

            var outputSelectOption = options.Where(m => m.Aliases.Any(n => n == $"-{outputModeOption.Value}")).FirstOrDefault();
            if (outputSelectOption == default)
            {

                var inputFile = inputOption.Value.First();
                var extension = $".{outputModeOption.Value}";
                var fileName = GetChangeExtension(inputFile, extension);
                
                return new[] { fileName };
            }
            else
            {
                var fileNames = outputSelectOption.DefaultFunc(options);
                
                return fileNames;
            }
        }

        /// <summary>
        /// デフォルトファイル名の取得
        /// </summary>
        /// <param name="options"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private static string[] GetDefaulFilename(IOption[] options, string extension)
        {
            var inputOption = (Option<FileInfo[]>)options.Where(m => m.Name == "input").FirstOrDefault();
            if (inputOption == default)
            {
                return Array.Empty<string>();
            }

            if (inputOption.Value == default || inputOption.Value.Length == 0)
            {
                return Array.Empty<string>();
            }

            var inputFile = inputOption.Value.First();
            var fileName = GetChangeExtension(inputFile, extension);

            return new[] { fileName };
        }

        private static string GetChangeExtension(FileInfo fileInfo, string extension)
        {
            if (fileInfo.Extension.ToUpper() == fileInfo.Extension)
            {
                extension = extension.ToUpper();
            }
            else
            {
                extension = extension.ToLower();
            }

            var fileName = Path.ChangeExtension(fileInfo.FullName, extension);

            return fileName;
        }

        /// <summary>
        /// ReadMe.MD
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLoadException"></exception>
        private static string ReadMe()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "AILZ80ASM.Documents.README.md";

            if (!assembly.GetManifestResourceNames().Any(m => m == resourceName))
            {
                throw new FileNotFoundException("リソースが見つかりませんでした。", resourceName);
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == default)
                {
                    throw new FileLoadException("リソースが読み込みできませんでした。", resourceName);
                }

                using (var reader = new StreamReader(stream))
                {
                    var readme = reader.ReadToEnd();

                    readme = Regex.Replace(readme, "^######", "□□", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^#####", "□□", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^####", "■■", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^###", "■■", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^##", "■", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^#", "■", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^- ", " ・ ", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^ - ", " ・ ", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^\t- ", " 　 → ", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "^```", $"{Environment.NewLine}{new String('-', 80)}{Environment.NewLine}", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, " \\*\\*", "", RegexOptions.Multiline);
                    readme = Regex.Replace(readme, "\\*\\* ", "", RegexOptions.Multiline);

                    return readme;
                }
            }
        }

        /// <summary>
        /// JsonからArgumentsのパースを行う
        /// </summary>
        /// <param name="profileString"></param>
        /// <returns></returns>
        public static IList<string> ParseArgumentsFromJsonString(string profileString)
        {
            var options = new JsonSerializerOptions { AllowTrailingCommas = true };
            var defaultProfile = JsonSerializer.Deserialize<AILZ80ASM.Models.Profile>(profileString, options);
            var profileArguments = new List<string>();
            profileArguments.AddRange(defaultProfile.DefaultOptions.SelectMany(m => m.Split(' ')).Where(m => !string.IsNullOrEmpty(m)));
            if (defaultProfile.DisableWarnings != default && defaultProfile.DisableWarnings.Count() > 0)
            {
                profileArguments.Add("-dw");
                profileArguments.AddRange(defaultProfile.DisableWarnings);
            }

            return profileArguments;
        }

    }
}
