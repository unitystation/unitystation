using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Util;

namespace US13.Systems.Faith.ProclamationGen
{
    public class CodedProclamationText : IFaithProclamationTextGenerator
    {
        [SerializeField] private List<string> inputStrings;
        [SerializeField] private int length = 3;

        public string GenerateProclamation()
        {
            return GenerateRandomCodedString();
        }

        public string GenerateRejection()
        {
            return GenerateRandomCodedString();
        }

        private string GenerateRandomCodedString()
        {
            if (inputStrings == null || inputStrings.Any() == false)
            {
                throw new InvalidOperationException("Input strings cannot be null or empty.");
            }

            if (inputStrings.Count <= length)
            {
	            throw new Exception("there are not enough input strings to generate a coded proclamation. " +
				                    $"Input strings count: {inputStrings.Count}, required length: {length}");
            }

            var newShuffledCode = "";
            for (int i = 0; i < length; i++)
            {
	            newShuffledCode += inputStrings.PickRandom() + " ";
            }
            newShuffledCode.TrimEnd();
            return newShuffledCode;
        }
    }
}