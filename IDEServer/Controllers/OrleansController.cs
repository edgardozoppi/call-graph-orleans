﻿using CodeGraphModel;
using ConsoleServer.Utils;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ConsoleServer.Controllers
{
	// http://localhost:14054/index.html?graph=ConsoleApplication1&mode=orleans
	public class OrleansController : ApiController
    {
		public const string ROOT_DIR = @"C:\Users\diegog\Source\ArcusClientPrototype\src\ArcusClient\data\";
		//public const string ROOT_DIR = @"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\";

		private static IDictionary<string, string> documentsAssemblyName;

		public static ISolutionManager SolutionManager { get; internal set; }

		static OrleansController()
		{
			documentsAssemblyName = new Dictionary<string, string>();
        }

		/// <summary>
		/// Get All Files in the graph (Abbreviated)
		/// </summary>
		[Route("api/orleans/{graph}/files")]
		public async Task<IEnumerable<FileResponse>> GetAllFilesAsync(string graph)
		{
			using (TimedLog.Time(graph + " :: Get All Files"))
			{				
				var providers = await SolutionManager.GetProjectCodeProvidersAsync();
				var result = new List<FileResponse>();

				foreach (var provider in providers)
				{
					var files = await provider.GetDocumentsAsync();

					files = from f in files
							where !FilterFile(f)
							select f;

					result.AddRange(files);
				}

				return result;
			}
		}

		private static bool FilterFile(FileResponse file)
		{
			// TODO: Hack!!!
			var filename = Path.GetFileName(file.filepath);
			if (filename.StartsWith(".NETFramework,")) return true;

			ProcessFileResponse(file);
			documentsAssemblyName[file.filepath] = file.assemblyname;
			return false;
		}

		private static void ProcessFileResponse(FileResponse file)
		{
			var buildInfo = new BuildInfo();

			file.filepath = FixFilePath(file.filepath);
			file.repository = buildInfo.RepositoryName;
			file.version = buildInfo.VersionName;

			if (file.referenceAnnotation != null)
			{
				foreach (var declaration in file.declarationAnnotation)
				{
					ProcessDeclarationAnnotation(declaration);
				}

				foreach (var reference in file.referenceAnnotation)
				{
					ProcessReferenceAnnotation(reference);
				}
			}
		}

		private static void ProcessAnnotation(Annotation annotation)
		{
			var buildInfo = new BuildInfo();

			annotation.declAssembly = string.Format("{0}/{1}", buildInfo.RepositoryName, buildInfo.BranchName);
        }

		private static void ProcessDeclarationAnnotation(DeclarationAnnotation declaration)
		{
			ProcessAnnotation(declaration);
		}

		private static void ProcessReferenceAnnotation(ReferenceAnnotation reference)
		{
			ProcessAnnotation(reference);

			reference.declFile = FixFilePath(reference.declFile);
		}

		private static void ProcessSymbolReference(SymbolReference reference)
		{
			var buildInfo = new BuildInfo();

			reference.preview = FixFilePath(reference.preview);
			reference.tref = string.Format("{0}/{1}/{2}", buildInfo.RepositoryName, buildInfo.BranchName, reference.preview);
		}

		private static string FixFilePath(string filePath)
		{
			if (filePath == null) return null;

			if (filePath.StartsWith(ROOT_DIR, StringComparison.InvariantCultureIgnoreCase))
			{
				filePath = filePath.Substring(ROOT_DIR.Length, filePath.Length - ROOT_DIR.Length);
			}

			filePath = filePath.Replace(@"\", "/");
			return filePath;
        }

		/// <summary>
		/// Get Full Files matching specified file path
		/// </summary>
		[Route("api/orleans/{graph}/entities/file/{*filePath}")]
		public async Task<IEnumerable<FileResponse>> GetFileEntitiesAsync(string graph, string filePath)
		{
			using (TimedLog.Time(graph + " :: Get File"))
			{
				var fullPath = Path.Combine(ROOT_DIR, filePath).Replace("/", @"\");
				var assemblyName = documentsAssemblyName[filePath];
				var provider = await SolutionManager.GetProjectCodeProviderAsync(assemblyName);
				var result = await provider.GetDocumentEntitiesAsync(fullPath);

				foreach (var file in result)
				{
					ProcessFileResponse(file);
				}

				return result;
			}
		}

		/// <summary>
		/// Get All references of symbol with specified Uid
		/// </summary>
		[Route("api/orleans/{graph}/references/{uid}")]
        public async Task<IEnumerable<SymbolReference>> GetReferencesAsync(string graph, string uid)
        {
            using (TimedLog.Time(graph + " :: Get References"))
            {
				var result = new List<SymbolReference>();

				if (uid.Contains('@'))
				{
					// Find all method definitions
					var uidparts = uid.Split('@');
					var methodId = uidparts[0];
					var invocationIndex = Convert.ToInt32(uidparts[1]);

					var methodDescriptor = MethodDescriptor.DeMarsall(methodId);
					var methodEntity = await SolutionManager.GetMethodEntityAsync(methodDescriptor);
					var callees = await methodEntity.GetCalleesAsync(invocationIndex);

					foreach (var callee in callees)
					{
						var provider = await SolutionManager.GetProjectCodeProviderAsync(callee);
						var reference = await provider.GetDeclarationInfoAsync(callee);
						//var calleeEntity = await SolutionManager.GetMethodEntityAsync(calleeDescriptor);
						//var reference = await calleeEntity.GetDeclarationInfoAsync();

						if (reference != null)
						{
							ProcessSymbolReference(reference);
							result.Add(reference);
						}
					}
				}
				else
				{
					// Find all method references
					var methodId = uid;
					var methodDescriptor = MethodDescriptor.DeMarsall(methodId);
					var methodEntity = await SolutionManager.GetMethodEntityAsync(methodDescriptor);
					var callers = await methodEntity.GetCallersAsync();

					foreach (var caller in callers)
					{
						var provider = await SolutionManager.GetProjectCodeProviderAsync(caller.Caller);
						var reference = await provider.GetInvocationInfoAsync(caller);

						if (reference != null)
						{
							ProcessSymbolReference(reference);
							result.Add(reference);
						}
					}

					//var callers = await methodEntity.GetCallersDeclarationInfoAsync();

					//foreach (var reference in callers)
					//{
					//	ProcessSymbolReference(reference);
					//	result.Add(reference);
					//}
				}

				return result;
			}
        }

        /// <summary>
        /// Get Most referenced of symbols
        /// </summary>
        [Route("api/orleans/{graph}/referencecount/{count}")]
        public Task<IEnumerable<SymbolReferenceCount>> GetReferenceCountsAsync(string graph, uint count)
        {
            using (TimedLog.Time(graph + " :: Reference Count"))
            {
				var result = new List<SymbolReferenceCount>();
				return Task.FromResult(result.AsEnumerable());
			}
        }

        /// <summary>
        /// Get Most Co-occurring symbols
        /// </summary>
        [Route("api/orleans/{graph}/co-occurs/file/{count}")]
        public Task<IEnumerable<SymbolReferenceCount>> GetCoOccursCountsAsync(string graph, uint count)
        {
            using (TimedLog.Time(graph + " :: Co-occurrence Count"))
            {
				var result = new List<SymbolReferenceCount>();
				return Task.FromResult(result.AsEnumerable());
			}
        }
    }
}
