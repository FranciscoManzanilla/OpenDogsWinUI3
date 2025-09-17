using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDogs.Helpers
{
    public class DdsPreviewHelper
    {
        public static async Task<string?> ExtraerTemporalXbtAsync(
           string extractorPath,
           string fatPath,
           string rutaDentroDelFat,
           string gameTag = "WD2") // "WD2" o "Legion"
        {
            if (!File.Exists(extractorPath))
                throw new FileNotFoundException("No se encontró el extractor", extractorPath);

            if (!File.Exists(fatPath))
                throw new FileNotFoundException("No se encontró el archivo .fat", fatPath);

            string tempDir = Path.Combine(Path.GetTempPath(), "preview_temp");
            string outputPath = Path.Combine(tempDir, Path.GetDirectoryName(rutaDentroDelFat)!);
            Directory.CreateDirectory(outputPath);

            string arguments;
            string? listaPath = null; // Solo se usará si es Legion

            if (gameTag.Equals("WATCH_DOGS2", StringComparison.OrdinalIgnoreCase))
            {
                arguments = $"-file \"{fatPath}\" \"{rutaDentroDelFat}\" \"{tempDir}\"";
            }
            else if (gameTag.Equals("Watch Dogs Legion", StringComparison.OrdinalIgnoreCase))
            {
                listaPath = Path.Combine(tempDir, "temp.txt");
                string rutaNormalizada = rutaDentroDelFat.Replace("/", "\\");
                await File.WriteAllTextAsync(listaPath, rutaNormalizada, Encoding.ASCII);

                arguments = $"-s \"{fatPath}\" \"{tempDir}\" \"{listaPath}\"";
            }
            else
            {
                throw new ArgumentException("gameTag debe ser 'WD2' o 'Legion'");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = extractorPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var tcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine("[EXTRACTOR] " + e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine("[ERROR] " + e.Data);
            };

            process.Exited += (s, e) => tcs.TrySetResult(true);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            // Borra el archivo txt si se creó
            if (listaPath != null && File.Exists(listaPath))
            {
                try { File.Delete(listaPath); }
                catch (Exception ex) { Console.WriteLine("[WARN] No se pudo borrar listaPath: " + ex.Message); }
            }

            string finalPath = Path.Combine(tempDir, rutaDentroDelFat.Replace("/", "\\"));
            return File.Exists(finalPath) ? finalPath : null;
        }


        public static async Task PlayVideo(string playerExe, string file)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = playerExe,
                    Arguments = $"binkplay \"{file}\" /X200 /Y300 /W800 /H600",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }

        public static async Task<string?> ExtraerConvertirYCargarXBG(string extractorPath, string fatPath, string rutaDentroDelFat, string blenderPath, string scriptConvertPath)
        {
            try
            {
                // await MostrarErrorUIAsync("Saludos", "Testeo", xamlRoot);

                // Validar archivos requeridos
                if (!File.Exists(extractorPath))
                    throw new FileNotFoundException("No se encontró WD2_Extractor.exe", extractorPath);

                if (!File.Exists(fatPath))
                    throw new FileNotFoundException("No se encontró el archivo .fat", fatPath);

                if (!File.Exists(blenderPath))
                    throw new FileNotFoundException("No se encontró Blender", blenderPath);

                if (!File.Exists(scriptConvertPath))
                    throw new FileNotFoundException("No se encontró convert_xbg_to_obj.py", scriptConvertPath);

                // Carpeta de salida para el .xbg extraído
                string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string salidaBase = Path.Combine(escritorio, "Watch_Dogs_2");
                string rutaXbgExtraida = Path.Combine(salidaBase, rutaDentroDelFat.Replace("/", "\\"));

                // Crear carpeta si no existe
                string? carpetaDestino = Path.GetDirectoryName(rutaXbgExtraida);
                if (!string.IsNullOrEmpty(carpetaDestino))
                    Directory.CreateDirectory(carpetaDestino);

                Debug.WriteLine("📦 Ejecutando extractor...");

                // Ejecutar extractor
                var extractor = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = extractorPath,
                        Arguments = $"-file \"{fatPath}\" \"{rutaDentroDelFat}\" \"{salidaBase}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                extractor.OutputDataReceived += (s, e) => Debug.WriteLine("Extractor stdout: " + e.Data);
                extractor.ErrorDataReceived += (s, e) => Debug.WriteLine("Extractor stderr: " + e.Data);

                extractor.Start();
                extractor.BeginOutputReadLine();
                extractor.BeginErrorReadLine();
                await extractor.WaitForExitAsync();

                Debug.WriteLine("✅ Extractor finalizado");

                if (!File.Exists(rutaXbgExtraida))
                {
                    Debug.WriteLine($"❌ No se encontró el archivo extraído: {rutaXbgExtraida}");
                    return null;
                }

                // Carpeta temporal para exportación
                string outputFolder = Path.Combine(Path.GetTempPath(), "WD2Preview");
                Directory.CreateDirectory(outputFolder);
                string objPath = Path.Combine(outputFolder, "model.obj");

                Debug.WriteLine("🎬 Ejecutando Blender...");

                var blender = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = blenderPath,
                        Arguments = $"--background --python \"{scriptConvertPath}\" -- \"{rutaXbgExtraida}\" \"{objPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var blenderOutput = new StringBuilder();
                var blenderError = new StringBuilder();

                blender.OutputDataReceived += (s, e) => { if (e.Data != null) blenderOutput.AppendLine(e.Data); };
                blender.ErrorDataReceived += (s, e) => { if (e.Data != null) blenderError.AppendLine(e.Data); };

                blender.Start();
                blender.BeginOutputReadLine();
                blender.BeginErrorReadLine();
                await blender.WaitForExitAsync();

                Debug.WriteLine("✅ Blender finalizado");

                bool blenderFallo = blender.ExitCode != 0 || !File.Exists(objPath);

                if (blenderFallo)
                {
                    Debug.WriteLine($"❌ Blender terminó con fallo. ExitCode: {blender.ExitCode}");
                    Debug.WriteLine("🔴 Blender stdout:\n" + blenderOutput);
                    Debug.WriteLine("🔴 Blender stderr:\n" + blenderError);

                  

                    return null;
                }

                Debug.WriteLine($"✅ OBJ generado correctamente en: {objPath}");
                return objPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("🧨 Excepción en ExtraerConvertirYCargarXBG: " + ex.ToString());
                return null;
            }
        }


        public static async Task<string?> GenDDS(string extractorPath, string fatPath, string rutaDentroDelFat)
        {
            if (!File.Exists(extractorPath))
                throw new FileNotFoundException("No se encontró WD2_Extractor.exe", extractorPath);

            if (!File.Exists(fatPath))
                throw new FileNotFoundException("No se encontró el archivo .fat", fatPath);

            // Carpeta de salida en el escritorio
            string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string salidaBase = Path.Combine(escritorio, "Watch_Dogs_2");
            string rutaSalidaXbt = Path.Combine(salidaBase, rutaDentroDelFat.Replace("/", "\\"));

            // Crear carpeta destino si no existe
            string? carpetaDestino = Path.GetDirectoryName(rutaSalidaXbt);
            if (!string.IsNullOrEmpty(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            // Ejecutar el extractor
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = extractorPath,
                    Arguments = $"-file \"{fatPath}\" \"{rutaDentroDelFat}\" \"{salidaBase}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (!File.Exists(rutaSalidaXbt))
                return null;

            try
            {
                DividirXbtEnHeaderYdds(rutaSalidaXbt);
                // Eliminar el .xbt original
                File.Delete(rutaSalidaXbt);

                string ddsPath = Path.ChangeExtension(rutaSalidaXbt, ".dds");
                return File.Exists(ddsPath) ? ddsPath : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error al dividir el archivo XBT: " + ex.Message);
                return rutaSalidaXbt;
            }
        }

        private static void DividirXbtEnHeaderYdds(string xbtPath)
        {
            byte[] fileBytes = File.ReadAllBytes(xbtPath);
            byte[] ddsSignature = Encoding.ASCII.GetBytes("DDS ");
            int ddsOffset = FindBytes(fileBytes, ddsSignature);

            if (ddsOffset == -1)
                throw new Exception("No se encontró la firma DDS en el archivo XBT.");

            // Separa header y DDS
            byte[] headerData = new byte[ddsOffset];
            byte[] ddsData = new byte[fileBytes.Length - ddsOffset];
            Array.Copy(fileBytes, 0, headerData, 0, ddsOffset);
            Array.Copy(fileBytes, ddsOffset, ddsData, 0, ddsData.Length);

            string outputFolder = Path.GetDirectoryName(xbtPath)!;
            string baseName = Path.GetFileNameWithoutExtension(xbtPath);

            // Guarda el header como .wdh
            string headerPath = Path.Combine(outputFolder, baseName + ".wdh");
            File.WriteAllBytes(headerPath, headerData);

            // Guarda el DDS
            string ddsPath = Path.Combine(outputFolder, baseName + ".dds");
            File.WriteAllBytes(ddsPath, ddsData);

            Console.WriteLine($"✅ Exportado: {ddsPath}");
        }



        public static async Task PlayWem(string playerExe, string file)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = playerExe,
                    Arguments = $"vgmstream-cli.exe \"{file}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }

        public static async Task<string?> ExtraerArchivoDesdeFatAsync(
            string extractorPath,
            string fatPath,
            string rutaDentroDelFat,
            string gameTag = "WD2")
        {
            if (!File.Exists(extractorPath))
                throw new FileNotFoundException("No se encontró el extractor", extractorPath);

            if (!File.Exists(fatPath))
                throw new FileNotFoundException("No se encontró el archivo .fat", fatPath);

            string tempDir = Path.Combine(Path.GetTempPath(), "preview_temp");
            Directory.CreateDirectory(tempDir);

            string arguments;
            string? listaPath = null;

            if (gameTag.Equals("WATCH_DOGS2", StringComparison.OrdinalIgnoreCase))
            {
                arguments = $"-file \"{fatPath}\" \"{rutaDentroDelFat}\" \"{tempDir}\"";
            }
            else if (gameTag.Equals("Watch Dogs Legion", StringComparison.OrdinalIgnoreCase))
            {
                listaPath = Path.Combine(tempDir, "temp.txt");
                string rutaNormalizada = rutaDentroDelFat.Replace("/", "\\");
                await File.WriteAllTextAsync(listaPath, rutaNormalizada, Encoding.ASCII);

                arguments = $"-s \"{fatPath}\" \"{tempDir}\" \"{listaPath}\"";
            }
            else
            {
                throw new ArgumentException("gameTag debe ser 'WD2' o 'Legion'");
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = extractorPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            // Limpieza temporal del archivo lista si fue Legion
            if (listaPath != null && File.Exists(listaPath))
            {
                try { File.Delete(listaPath); } catch { }
            }

            // Ruta al archivo extraído (sin convertir ni procesar)
            string finalPath = Path.Combine(tempDir, rutaDentroDelFat.Replace("/", "\\"));
            return File.Exists(finalPath) ? finalPath : null;
        }



        public static byte[]? ExtractDdsToMemory(string xbtPath)
        {
            byte[] fileBytes = File.ReadAllBytes(xbtPath);
            byte[] ddsSignature = Encoding.ASCII.GetBytes("DDS ");
            int ddsOffset = FindBytes(fileBytes, ddsSignature);

            if (ddsOffset == -1)
                return null;

            // Devuelve solo la parte DDS del archivo
            return fileBytes.Skip(ddsOffset).ToArray();
        }

        public static int FindBytes(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return i;
            }
            return -1;
        }

        public static byte[] Extract(string xbtPath, string outputFolder)
        {
            byte[] fileBytes = File.ReadAllBytes(xbtPath);
            byte[] ddsSignature = Encoding.ASCII.GetBytes("DDS ");
            int ddsOffset = FindBytes(fileBytes, ddsSignature);

            if (ddsOffset == -1)
                throw new Exception("No se encontró la firma DDS en el archivo XBT.");

            // Separar header y DDS
            byte[] headerData = new byte[ddsOffset];
            byte[] ddsData = new byte[fileBytes.Length - ddsOffset];
            Array.Copy(fileBytes, 0, headerData, 0, ddsOffset);
            Array.Copy(fileBytes, ddsOffset, ddsData, 0, ddsData.Length);

            string baseName = Path.GetFileNameWithoutExtension(xbtPath);
            string tempFolder = Path.Combine(outputFolder, "tempDDS");
            Directory.CreateDirectory(tempFolder);

            string headerPath = Path.Combine(tempFolder, baseName + ".wdh");
            string ddsPath = Path.Combine(tempFolder, baseName + ".dds");

            // Guardar archivos temporales
            File.WriteAllBytes(headerPath, headerData);
            File.WriteAllBytes(ddsPath, ddsData);

            // Leer y retornar el contenido del DDS
            byte[] ddsBytes = File.ReadAllBytes(ddsPath);

            // Borrar el archivo DDS después de cargarlo a memoria
            File.Delete(ddsPath);
            File.Delete(headerPath);

            return ddsBytes;
        }
    }
}
