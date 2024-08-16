using System.Security.Cryptography;
using BerechitChatGPT.Services;
using Polly;

namespace BerechitChatGPT
{
	public class Program
	{
		private static readonly string ModelUrl = "https://huggingface.co/TheBloke/phi-2-GGUF/resolve/main/phi-2.Q4_K_M.gguf";
		private static readonly int RetryCount = 5;
		private static readonly string ExpectedSha256 = "324356668fa5ba9f4135de348447bb2bbe2467eaa1b8fcfb53719de62fbd2499";
		public static string ModelPath { get; private set; } // Public static variable accessible from controllers

		public static async Task Main(string[] args)
		{
			// Define the model path relative to the application directory
			var modelDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModelsLamma");
			ModelPath = Path.Combine(modelDirectory, "phi-2.Q4_K_M.gguf");

			// Download the model during application startup
		//	await DownloadModel(ModelPath);

			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container
			builder.Services.AddControllersWithViews();



			builder.Services.AddSingleton<StatefulChatService>();
			builder.Services.AddScoped<StatelessChatService>();


			var app = builder.Build();

			// Configure the HTTP request pipeline
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
			}
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

			app.Run();
		}

		private static async Task DownloadModel(string modelPath)
		{
			if (File.Exists(modelPath))
			{
				Console.WriteLine("Model already exists at the specified path.");

				// Verify the SHA256 hash of the existing file
				if (await VerifyFileHash(modelPath))
				{
					Console.WriteLine("File integrity verified.");
					return; // If the file already exists and hash matches, skip the download.
				}

				Console.WriteLine("File integrity check failed. Redownloading the model.");
				File.Delete(modelPath); // Delete the corrupted file before retrying download.
			}

			var policy = Policy
				.Handle<HttpRequestException>()
				.Or<Exception>(ex => !(ex is HttpRequestException))
				.WaitAndRetryAsync(RetryCount, retryAttempt => TimeSpan.FromSeconds(0.5), (exception, timeSpan, retry, ctx) =>
				{
					Console.WriteLine($"Retry {retry} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
				});

			await policy.ExecuteAsync(async () =>
			{
				using var httpClient = new HttpClient();
				var response = await httpClient.GetAsync(ModelUrl);
				response.EnsureSuccessStatusCode();
				var fileData = await response.Content.ReadAsByteArrayAsync();

				// Ensure the directory exists before saving the file
				var directoryPath = Path.GetDirectoryName(modelPath);
				if (!Directory.Exists(directoryPath))
				{
					Directory.CreateDirectory(directoryPath);
				}

				await File.WriteAllBytesAsync(modelPath, fileData);
				Console.WriteLine("Model downloaded successfully.");

				// Verify the SHA256 hash after download
				if (!await VerifyFileHash(modelPath))
				{
					Console.WriteLine("Downloaded file hash does not match expected hash. Deleting file and retrying.");
					File.Delete(modelPath);
					throw new Exception("File hash mismatch after download.");
				}
			});
		}

		private static async Task<bool> VerifyFileHash(string filePath)
		{
			using var sha256 = SHA256.Create();
			using var fileStream = File.OpenRead(filePath);
			var hashBytes = await sha256.ComputeHashAsync(fileStream);
			var fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

			return fileHash == ExpectedSha256;
		}
	}
}
