using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Threads
{
    class Program
    {
        private static int ThreadCounts = 5;
        private static string TxtFilePath = "";
        private static ManualResetEvent WordsProcessDone = new(false);
        private static Dictionary<string, string> Sentences = new();
        public static ConcurrentDictionary<string, int> WordDictionary = new();

        static void Main(string[] args)
        {


            var paragraph =File.ReadAllText( args[0]);

            Console.WriteLine("Please insert TreadCount value:");

            SetTreadLineValue();

            var sentenceStartIndex = 0;
            var wordCount = String.IsNullOrEmpty(paragraph) ? 0 : 1;
            for (var p = 0; p < paragraph.Length; p++)
            {
                var currentChar = paragraph[p];


                switch (currentChar)
                {
                    case ' ':
                        wordCount++;
                        break;
                    case '.':
                    case '?':
                    case '!':
                        {
                            var sentence = paragraph.Substring(sentenceStartIndex, p - sentenceStartIndex);
                            sentenceStartIndex = p + 1;
                            Sentences[sentence] = sentence;
                            break;
                        }
                }
            }

            if (wordCount == 0)
            {
                Console.WriteLine("No sentences(s) found");
                Console.ReadLine();
            }

            Console.WriteLine($"Total Sentences -> {Sentences.Count}");
            Console.WriteLine($"Total words -> {wordCount}");

            var workers = new List<SentenceProcessor>();

            for (var j = 0; j < ThreadCounts; j++)
            {
                var worker = new SentenceProcessor();
                worker.SentenceProcessed += WorkerOnSentenceProcessed;
                workers.Add(worker);
                Task.Run(() => worker.Start());
            }
            

            for (var i = 0; i < Sentences.Count; i++)
            {
                var currentWorker = (i % workers.Count);
                var worker = workers.ElementAt(currentWorker);
                var sentence = Sentences.Keys.ElementAt(i);
                worker.AddSentence(sentence);
            }


            WordsProcessDone.WaitOne();


            workers.ForEach(f =>
            {
                f.SentenceProcessed -= WorkerOnSentenceProcessed;
                f.Stop();
            });

            var sorted = WordDictionary
                .OrderByDescending(o => o.Value)
                .ThenByDescending(t => t.Key);

            foreach (var (key, value) in sorted)
            {
                Console.WriteLine($"{key} - {value}");
            }

            Console.ReadLine();
        }

      
        public static void SetTreadLineValue()
        {
            string sThreadCounts = Console.ReadLine();
            if (!Int32.TryParse(sThreadCounts, out ThreadCounts))
            {
                Console.WriteLine("Please insert only numeric value for TreadCount value:");
                SetTreadLineValue();
            }
        }

        private static void WorkerOnSentenceProcessed(object sender, SentenceProcessedArg e)
        {
            lock (Sentences)
            {
                if (e.Words != null)
                {
                    foreach (var word in e.Words)
                    {
                        WordDictionary.TryGetValue(word, out var count);
                        WordDictionary[word] = count + 1;
                    }
                }

                Sentences.Remove(e.Sentence, out _);
                if (Sentences.Count == 0)
                {
                    WordsProcessDone.Set();
                }
            }
        }
    }
}