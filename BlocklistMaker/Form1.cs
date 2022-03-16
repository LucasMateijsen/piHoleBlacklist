using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading.Tasks.Dataflow;
using System.Threading;
using System.IO;

namespace BlocklistMaker
{
    public partial class Form1 : Form
    {
        private List<string> MainList = null;
        private BindingList<BlockListFileList> BlockListItems;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            BlocklistHandler.GetBlocklists().ForEach(bl => dataGridView1.Rows.Add(bl.Item1, bl.Item2));
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            var uiUpdateThread = new Thread(UpdateUi);
            uiUpdateThread.Start();

            var thread = new Thread(new ParameterizedThreadStart(Run));
            thread.Start(uiUpdateThread);
        }

        private void Run(object uiUpdateThread)
        {
            FetchMainList();

            var additionalLists = FetchAndProcess();
            IronListAndSaveToFile(additionalLists);

            (uiUpdateThread as Thread).Abort();
        }

        private void UpdateUi()
        {
            while(true)
            {
                var source = new BindingSource(BlockListItems, null);
                dataGridViewListFile.Invoke((Action)delegate ()
                {
                    dataGridViewListFile.DataSource = source;
                });
                Thread.Sleep(500);
            }
        }

        private void FetchMainList()
        {
            MainList = new List<string>();
            var downloadBlockList = new TransformBlock<string, string>(url =>
            {
                return HttpHandler.Fetch(url);
            });

            var convertToList = new TransformManyBlock<string, string>(listAsString =>
            {
                return BlocklistHandler.ToList(listAsString);
            });

            var removeCommentsAndEmpty = new TransformBlock<string, string>(record =>
            {
                return BlocklistHandler.RemoveComments(record);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1000 });

            var addToMainList = new ActionBlock<string>(record =>
            {
                if (record != string.Empty)
                {
                    MainList.Add(record);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            downloadBlockList.LinkTo(convertToList, new DataflowLinkOptions { PropagateCompletion = true });
            convertToList.LinkTo(removeCommentsAndEmpty, new DataflowLinkOptions { PropagateCompletion = true });
            removeCommentsAndEmpty.LinkTo(addToMainList, new DataflowLinkOptions { PropagateCompletion = true });

            downloadBlockList.Post(dataGridView1.Rows[0].Cells[0].Value.ToString());
            downloadBlockList.Complete();
            addToMainList.Completion.Wait();

            label1.Invoke((Action)delegate()
            {
                label1.Text = $"Fetched main hostfile with {MainList.Count()} lines.";
            });
        }

        private List<string> FetchAndProcess()
        {
            var strt = DateTime.Now;
            var additionalBlockLists = new List<string>();
            BlockListItems = new BindingList<BlockListFileList>();
            var src = new BindingSource(BlockListItems, null);
            dataGridViewListFile.Invoke((Action)delegate ()
            {
                dataGridViewListFile.DataSource = src;
            });

            var mainListMod = MainList.ToList();
            mainListMod.RemoveRange(0, 14);
            mainListMod = BlocklistHandler.RemoveDestination2(mainListMod).ToList();

            var updateRawDomainCount = new ActionBlock<BlockListFileList>(data =>
            {
                var itemEntry = BlockListItems.First(x => x.Url == data.Url);
                itemEntry.RawDomains = Convert.ToInt32(data.RawDomains);
                itemEntry.FilteredDomains = data.FilteredDomains;
            });
            var addFilteredDomainCount = new ActionBlock<string>(url =>
            {
                var itemEntry = BlockListItems.First(x => x.Url == url);
                itemEntry.FilteredDomains = itemEntry.FilteredDomains + 1;
            });

            var downloadBlockList = new TransformBlock<string, Tuple<string, string>>(url =>
            {
                return new Tuple<string, string>(url, HttpHandler.Fetch(url));
            });

            var convertToList = new TransformManyBlock<Tuple<string, string>, Tuple<string, string>>(listAsString =>
            {
                var asList = BlocklistHandler.ToList(listAsString.Item2);
                var updateData = new BlockListFileList(listAsString.Item1, asList.Length);
                updateRawDomainCount.Post(updateData);

                var returnVal = asList.Select(x => new Tuple<string, string>(listAsString.Item1, x));
                return returnVal;
            });

            var removeCommentsAndEmpty = new TransformBlock<Tuple<string, string>, Tuple<string, string>>(record =>
            {
                return new Tuple<string, string>(record.Item1, BlocklistHandler.RemoveComments(record.Item2));
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1000 });

            var removeDestinationForLines = new TransformBlock<Tuple<string, string>, Tuple<string, string>>(record =>
            {
                return new Tuple<string, string>(record.Item1, BlocklistHandler.RemoveDestination(record.Item2));
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1000 });

            var addIfNotDuplicate = new ActionBlock<Tuple<string, string>>(record =>
            {
                if (!mainListMod.Contains(record.Item2) && !string.IsNullOrWhiteSpace(record.Item2))
                {
                    lock(additionalBlockLists)
                    {
                        additionalBlockLists.Add(record.Item2);
                        addFilteredDomainCount.Post(record.Item1);
                    }
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1000 });

            downloadBlockList.LinkTo(convertToList, new DataflowLinkOptions { PropagateCompletion = true });
            convertToList.LinkTo(removeCommentsAndEmpty, new DataflowLinkOptions { PropagateCompletion = true});
            removeCommentsAndEmpty.LinkTo(removeDestinationForLines, new DataflowLinkOptions { PropagateCompletion = true });
            removeDestinationForLines.LinkTo(addIfNotDuplicate, new DataflowLinkOptions { PropagateCompletion = true });

            var urls = GetUrls();
            foreach (var url in urls)
            {
                downloadBlockList.Post(url);
            }

            downloadBlockList.Complete();
            addIfNotDuplicate.Completion.Wait();
            updateRawDomainCount.Complete();
            updateRawDomainCount.Completion.Wait();
            addFilteredDomainCount.Complete();
            addFilteredDomainCount.Completion.Wait();

            additionalBlockLists = additionalBlockLists.Distinct().ToList();
            additionalBlockLists.Sort();
            var source = new BindingSource(BlockListItems, null);
            dataGridViewListFile.Invoke((Action)delegate ()
            {
                dataGridViewListFile.DataSource = source;
            });

            label2.Invoke((Action)delegate ()
            {
                label2.Text = $"Found {additionalBlockLists.Count} records using {urls.Count} additional lists.";
            });
            var ttr = DateTime.Now.Subtract(strt).TotalMilliseconds;

            label2.Invoke((Action)delegate ()
            {
                label3.Text = $"Processing took: {ttr} milliseconds";
            });

            return additionalBlockLists;
        }

        private void IronListAndSaveToFile(List<string> blockList)
        {
            var asHostFile = blockList.Select(row =>
            {
                if (row.StartsWith("::"))
                {
                    return row;
                }
                return $"0.0.0.0 {row}";
            });

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\blocklist.txt",
                  string.Join("\r\n", asHostFile));
        }

        private List<string> GetUrls()
        {
            var urls = new List<string>();
            if (!checkBox1.Checked)
            {
                for (int i = 1; i < dataGridView1.Rows.Count - 1; i++)
                {
                    urls.Add(dataGridView1.Rows[i].Cells[0].Value.ToString());
                }
            }
            else
            {

                var downloadBlockList = new TransformBlock<string, string>(url =>
                {
                    return HttpHandler.Fetch(url);
                });

                var convertToList = new ActionBlock<string>(listAsString =>
                {
                    var lists = BlocklistHandler.ToList(listAsString);
                    urls.AddRange(lists);
                });
                downloadBlockList.LinkTo(convertToList, new DataflowLinkOptions { PropagateCompletion = true });

                downloadBlockList.Post(textBox1.Text);
                downloadBlockList.Complete();
                convertToList.Completion.Wait();
            }

            dataGridViewListFile.Invoke((Action)delegate ()
            {
                urls.ForEach(x =>
                {
                    BlockListItems.Add(new BlockListFileList(x));
                });
            });

            return urls;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Visible = checkBox1.Checked;
            dataGridView1.Visible = !checkBox1.Checked;
        }
    }
}
