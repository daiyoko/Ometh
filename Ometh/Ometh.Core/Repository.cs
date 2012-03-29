﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NGit;
using NGit.Api;
using NGit.Revwalk;
using NGit.Storage.File;

namespace Ometh.Core
{
    public class Repository
    {
        private readonly Git git;
        private readonly List<Commit> commits;

        public IEnumerable<Commit> Commits
        {
            get { return this.commits; }
        }

        public Repository(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            this.git = new Git(new FileRepository(Path.Combine(path, ".git")));
            this.commits = new List<Commit>();
        }

        public static bool IsValidRepository(string path)
        {
            // TODO: Check if the .git directory is actually a repository
            return Directory.Exists(Path.Combine(path, ".git"));
        }

        public string GetFullMessage(string hash)
        {
            var walk = new RevWalk(this.git.GetRepository());

            RevCommit commit = walk.ParseCommit(ObjectId.FromString(hash));

            walk.Dispose();

            return commit.GetFullMessage();
        }

        public void Load()
        {
            this.LoadCommits();
            this.LoadRefs();
        }

        private void LoadCommits()
        {
            var enumerable = this.git
                .Log()
                .Call()
                .Select(this.ToCommit);

            foreach (Commit commit in enumerable)
            {
                this.commits.Add(commit);
            }
        }

        private void LoadRefs()
        {
            var tags = this.git
                .GetRepository()
                .GetAllRefs()
                .Where(reference => reference.Key != "HEAD")
                .ToDictionary(entry => entry.Key, entry => entry.Value.GetObjectId().Name);

            foreach (KeyValuePair<string, string> pair in tags)
            {
                var commit = this.commits.FirstOrDefault(p => p.Hash == pair.Value);

                if (commit != null)
                {
                    commit.AddReference(new Reference(pair.Key));
                }

                else
                {
                    Debug.WriteLine("Could not find commit " + pair.Value);
                }
            }
        }

        private Commit ToCommit(RevCommit commit)
        {
            return new Commit
            (
                this,
                commit.Name,
                commit.GetShortMessage(),
                ToPerson(commit.GetAuthorIdent()),
                ToPerson(commit.GetCommitterIdent()),
                commit.GetCommitterIdent().GetWhen(),
                commit.Parents.Select(p => p.Name)
            );
        }

        private static Person ToPerson(PersonIdent ident)
        {
            return new Person(ident.GetName(), ident.GetEmailAddress());
        }
    }
}