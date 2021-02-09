using System.IO;
using System.IO.Abstractions;
using Google.Protobuf;

namespace MLAgents
{
    /// <summary>
    /// Responsible for writing demonstration data to file.
    /// </summary>
    public class DemonstrationStore
    {
        public const int MetaDataBytes = 32; // Number of bytes allocated to metadata in demo file.
        private readonly IFileSystem m_FileSystem;
        private const string k_DemoDirecory = "Assets/Demonstrations/";
        private const string k_ExtensionType = ".demo";

        private string m_FilePath;
        private DemonstrationMetaData m_MetaData;
        private Stream m_Writer;
        private float m_CumulativeReward;

        public DemonstrationStore(IFileSystem fileSystem)
        {
            m_FileSystem = fileSystem;
        }

        public DemonstrationStore()
        {
            m_FileSystem = new FileSystem();
        }

        /// <summary>
        /// Initializes the Demonstration Store, and writes initial data.
        /// </summary>
        public void Initialize(
            string demonstrationName, BrainParameters brainParameters, string brainName)
        {
            CreateDirectory();
            CreateDemonstrationFile(demonstrationName);
            WriteBrainParameters(brainName, brainParameters);
        }

        /// <summary>
        /// Checks for the existence of the Demonstrations directory
        /// and creates it if it does not exist.
        /// </summary>
        private void CreateDirectory()
        {
            if (!m_FileSystem.Directory.Exists(k_DemoDirecory))
            {
                m_FileSystem.Directory.CreateDirectory(k_DemoDirecory);
            }
        }

        /// <summary>
        /// Creates demonstration file.
        /// </summary>
        private void CreateDemonstrationFile(string demonstrationName)
        {
            // Creates demonstration file.
            var literalName = demonstrationName;
            m_FilePath = k_DemoDirecory + literalName + k_ExtensionType;
            var uniqueNameCounter = 0;
            while (m_FileSystem.File.Exists(m_FilePath))
            {
                literalName = demonstrationName + "_" + uniqueNameCounter;
                m_FilePath = k_DemoDirecory + literalName + k_ExtensionType;
                uniqueNameCounter++;
            }

            m_Writer = m_FileSystem.File.Create(m_FilePath);
            m_MetaData = new DemonstrationMetaData {demonstrationName = demonstrationName};
            var metaProto = m_MetaData.ToProto();
            metaProto.WriteDelimitedTo(m_Writer);
        }

        /// <summary>
        /// Writes brain parameters to file.
        /// </summary>
        private void WriteBrainParameters(string brainName, BrainParameters brainParameters)
        {
            // Writes BrainParameters to file.
            m_Writer.Seek(MetaDataBytes + 1, 0);
            var brainProto = brainParameters.ToProto(brainName, false);
            brainProto.WriteDelimitedTo(m_Writer);
        }

        /// <summary>
        /// Write AgentInfo experience to file.
        /// </summary>
        public void Record(AgentInfo info)
        {
            // Increment meta-data counters.
            m_MetaData.numberExperiences++;
            m_CumulativeReward += info.reward;
            if (info.done)
            {
                EndEpisode();
            }

            // Write AgentInfo to file.
            var agentProto = info.ToProto();
            agentProto.WriteDelimitedTo(m_Writer);
        }

        /// <summary>
        /// Performs all clean-up necessary
        /// </summary>
        public void Close()
        {
            EndEpisode();
            m_MetaData.meanReward = m_CumulativeReward / m_MetaData.numberEpisodes;
            WriteMetadata();
            m_Writer.Close();
        }

        /// <summary>
        /// Performs necessary episode-completion steps.
        /// </summary>
        private void EndEpisode()
        {
            m_MetaData.numberEpisodes += 1;
        }

        /// <summary>
        /// Writes meta-data.
        /// </summary>
        private void WriteMetadata()
        {
            var metaProto = m_MetaData.ToProto();
            var metaProtoBytes = metaProto.ToByteArray();
            m_Writer.Write(metaProtoBytes, 0, metaProtoBytes.Length);
            m_Writer.Seek(0, 0);
            metaProto.WriteDelimitedTo(m_Writer);
        }
    }
}
