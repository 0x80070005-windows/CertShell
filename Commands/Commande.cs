using System.Linq.Expressions;
using CertShell.Config;
using CertShell.Platform;
using CertShell.Security;

namespace CertShell.Commands;

public static class Commande
{
	private static string MountPoint => AppConfig.Instance.MountPoint;
	private static string ImgPath => AppConfig.Instance.ImgPath;
	private static string CertGuardPath => AppConfig.Instance.CertGuardPath;
	private static string CacheDir => PlatformHelper.CacheDir;

	// ===================== ls / clear =====================

	public static void LsCommande(string directory)
	{
		try
		{
			if (!Directory.Exists(directory)) return;
			foreach (var item in Directory.GetFileSystemEntries(directory))
				Console.WriteLine("\t" + Path.GetFileName(item));
			Console.WriteLine();
		}
		catch (UnauthorizedAccessException) { Console.WriteLine("Нет доступа к директории."); }
		catch (Exception ex) { Console.WriteLine($"Ошибка чтения: {ex.Message}"); }
	}

	public static void ClearCommande() => Console.Clear();

	public static void LsDirectory()
	{
		try
		{
			Console.WriteLine();
			if (Directory.Exists(MountPoint))
				foreach (var dir in Directory.GetDirectories(MountPoint))
					Console.WriteLine(Path.GetFileName(dir));
			Console.WriteLine();
		}
		catch (UnauthorizedAccessException) { Console.WriteLine("Нет доступа к директории."); }
		catch (Exception ex) { Console.WriteLine($"Ошибка чтения: {ex.Message}"); }
	}

	// ===================== mount / umount =====================

	public static void MountImg()
	{
		PlatformHelper.MountImg(ImgPath, MountPoint);
		AuditLog.Info($"mount img={ImgPath} point={MountPoint}");
	}

	// ===================== add / remove =====================

	public static void AddObject(string inputDirectory)
	{
		PlatformHelper.CopyFileToDirectory(inputDirectory, MountPoint);
		Console.WriteLine();
	}

	public static void RemoveObject(string inputDirectory)
	{
		PlatformHelper.DeleteFile(Path.Combine(MountPoint, inputDirectory));
	}

	// ===================== Кэш =====================

	public static void RemoveCache()
	{
		TryDelete(Path.Combine(CacheDir, "file_list.txt"));
		TryDelete(Path.Combine(CacheDir, "name_directory"));
		TryDelete(Path.Combine(CacheDir, "cd"));
	}

	private static void TryDelete(string path)
	{
		try { if (File.Exists(path)) File.Delete(path); } catch { }
	}

	// ===================== Запуск CertGuard =====================

	public static void LaunchCertguard(string cryptFile)
	{
		try
		{
			string encPath = Path.Combine(MountPoint, cryptFile);
			string decPath = encPath.EndsWith(".enc", StringComparison.Ordinal)
				? encPath[..^4]
				: encPath;

			// Убираем возможный «хвост» от прошлого запуска
			if (File.Exists(decPath))
			{
				try { File.Delete(decPath); } catch { }
			}

			if (!File.Exists(encPath))
			{
				Console.WriteLine($"Файл не найден: {encPath}");
				return;
			}

			AuditLog.Info($"open file={cryptFile}");

			bool started = PlatformHelper.LaunchInTerminal(CertGuardPath, encPath);
			if (!started)
			{
				Console.WriteLine("Не удалось запустить CertGuard.");
				return;
			}

			if (!PlatformHelper.WaitForFileStable(decPath, 600))
			{
				Console.WriteLine($"Файл {decPath} не появился. CertGuard не завершился?");
				AuditLog.Warn($"decrypt_timeout file={cryptFile}");
				return;
			}

			DefiningExtensions.OpenFile(Path.Combine(MountPoint, cryptFile));
		}
		catch (Exception ex)
		{
			AuditLog.Error($"launch_certguard_failed: {ex.Message}");
			Console.WriteLine($"Ошибка запуска CertGuard: {ex.Message}");
		}
	}

		// ===================== Cleanup при выходе =====================
public static void RemoveVideoEncrypt()
	{
		try
		{
			if (!Directory.Exists(MountPoint)) return;

			string[] patterns = { "*.mp4", "*.avi", "*.mkv", "*.mov" };
			foreach (var pattern in patterns)
			{
				foreach (var file in Directory.GetFiles(MountPoint, pattern))
					try
					{
						File.Delete(file);
					}

					catch { }
			}
		}

		catch { }
	}

public static void RemovePictureEncrypt()
	{
		try
		{
			if (!Directory.Exists(MountPoint)) return;

			string[] patterns = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.tif", "*.tiff", "*.webp" };
			foreach (var pattern in patterns)
			{
				foreach (var file in Directory.GetFiles(MountPoint, pattern))
					try
					{
						File.Delete(file);
					}

					catch { }
			}
		}

		catch { }
	}

public static void RemoveTextEncrypt()
	{
		try
		{
			if (!Directory.Exists(MountPoint)) return;

			string[] patterns = { ".txt", ".log" };
			foreach (var pattern in patterns)
			{
				foreach (var file in Directory.GetFiles(MountPoint, pattern))
					try
					{
						File.Delete(file);
					}

					catch { }
					
			}

		}

		catch { }
	}

public static void RemoveArchiveEncrypt()
	{

		Console.WriteLine(MountPoint);
		try
		{
			if (!Directory.Exists(MountPoint)) return ;
			{
				string[] patterns = { "*.zip" , "*.rar" , "*.tar" , "*.tgz" , "*.iso" , "*.img"};
				foreach (string pattern in patterns)
				{
					foreach (var file in Directory.GetFiles(MountPoint, pattern))
						try
						{
							File.Delete(file);
						}

						catch { }
				}	
			}

			
		}

		catch { }
	}



	public static void ExitToProgram()
	{
		AuditLog.Info("session_exit");
		RemoveVideoEncrypt();
		RemovePictureEncrypt();
		RemoveTextEncrypt();
		RemoveArchiveEncrypt();

		RemoveCache();

		Commands.ChangeDirectory.RemoveCache();

		PlatformHelper.CheckFuser(MountPoint);
		PlatformHelper.Umount(ImgPath);

		Environment.Exit(0);
	}

	// ===================== Диспетчер =====================

	public static void CallOfSystemCommands(string commandeLine)
	{
		switch (commandeLine)
		{
			case "ls": LsCommande(MountPoint); break;
			case "clear": ClearCommande(); break;
			case "add":
				Console.Write("Введите исходную директорию - ");
				string? addPath = Console.ReadLine();
				if (!string.IsNullOrEmpty(addPath) && File.Exists(addPath))
					AddObject(addPath);
				else
					Console.WriteLine("Файл не найден");
				break;
			case "remove":
				Console.Write("Введите название файла для удаления - ");
				string? remFile = Console.ReadLine();
				if (!string.IsNullOrEmpty(remFile) && File.Exists(Path.Combine(MountPoint, remFile)))
					RemoveObject(remFile);
				else
					Console.WriteLine("Файл не найден");
				break;
			case "exit": ExitToProgram(); break;
			case "mount": MountImg(); break;
			case "lsdir": LsDirectory(); break;
		}
	}
}
