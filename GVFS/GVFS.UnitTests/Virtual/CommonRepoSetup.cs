﻿using GVFS.Common;
using GVFS.Common.Git;
using GVFS.Common.Physical.Git;
using GVFS.UnitTests.Mock.Common;
using GVFS.UnitTests.Mock.Physical.FileSystem;
using GVFS.UnitTests.Mock.Physical.Git;
using NUnit.Framework;
using System.IO;

namespace GVFS.UnitTests.Virtual
{
    public class CommonRepoSetup
    {
        public CommonRepoSetup()
        {
            MockTracer tracer = new MockTracer();

            string gitBinPath = GitProcess.GetInstalledGitBinPath();
            if (string.IsNullOrWhiteSpace(gitBinPath))
            {
                Assert.Fail("Failed to find git.exe");
            }

            string enlistmentRoot = @"mock:\GVFS\UnitTests\Repo";
            GVFSEnlistment enlistment = new GVFSEnlistment(enlistmentRoot, "fake://repoUrl", "fake://cacheUrl", gitBinPath, null);

            this.GitParentPath = enlistment.WorkingDirectoryRoot;
            this.GVFSMetadataPath = enlistment.DotGVFSRoot;

            MockDirectory enlistmentDirectory = new MockDirectory(
                enlistmentRoot,
                new MockDirectory[]
                {
                    new MockDirectory(this.GitParentPath, folders: null, files: null),
                },
                null);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git\\config"), ".git config Contents", createDirectories: true);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git\\HEAD"), ".git HEAD Contents", createDirectories: true);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git\\logs\\HEAD"), "HEAD Contents", createDirectories: true);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git\\info\\always_exclude"), "always_exclude Contents", createDirectories: true);
            enlistmentDirectory.CreateDirectory(Path.Combine(this.GitParentPath, ".git\\objects\\pack"));

            MockFileSystem fileSystem = new MockFileSystem(enlistmentDirectory);
            this.Repository = new MockGitRepo(
                tracer, 
                enlistment,
                fileSystem);
            CreateStandardGitTree(this.Repository);

            this.Context = new GVFSContext(tracer, fileSystem, this.Repository, enlistment);

            this.HttpObjects = new MockHttpGitObjects(tracer, enlistment);
            this.GitObjects = new MockGVFSGitObjects(this.Context, this.HttpObjects);
        }
        
        public GVFSContext Context { get; private set; }

        public string GitParentPath { get; private set; }
        
        public string GVFSMetadataPath { get; private set; }
        public GVFSGitObjects GitObjects { get; private set; }

        public MockGitRepo Repository { get; private set; }
        public MockHttpGitObjects HttpObjects { get; private set; }
        
        private static void CreateStandardGitTree(MockGitRepo repository)
        {
            string rootSha = repository.GetHeadTreeSha();

            string atreeSha = repository.AddChildTree(rootSha, "A");
            repository.AddChildBlob(atreeSha, "A.1.txt", "A.1 in GitTree");
            repository.AddChildBlob(atreeSha, "A.2.txt", "A.2 in GitTree");

            string btreeSha = repository.AddChildTree(rootSha, "B");
            repository.AddChildBlob(btreeSha, "B.1.txt", "B.1 in GitTree");

            string dupContentSha = repository.AddChildTree(rootSha, "DupContent");
            repository.AddChildBlob(dupContentSha, "dup1.txt", "This is some duplicate content");
            repository.AddChildBlob(dupContentSha, "dup2.txt", "This is some duplicate content");

            string dupTreeSha = repository.AddChildTree(rootSha, "DupTree");
            repository.AddChildBlob(dupTreeSha, "B.1.txt", "B.1 in GitTree");
            
            repository.AddChildBlob(rootSha, "C.txt", "C in GitTree");
        }
    }
}