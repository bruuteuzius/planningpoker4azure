﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Duracellko.PlanningPoker.Domain.Serialization
{
    /// <summary>
    /// Object serializes and deserializes ScrumTeam objects to/from JSON text.
    /// </summary>
    public class ScrumTeamSerializer
    {
        private static readonly IContractResolver _contractResolver = new ScrumTeamContractResolver();
        private readonly DateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrumTeamSerializer"/> class.
        /// </summary>
        /// <param name="dateTimeProvider">The date time provider to provide current time. If null is specified, then default date time provider is used.</param>
        public ScrumTeamSerializer(DateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider ?? DateTimeProvider.Default;
        }

        /// <summary>
        /// Serializes ScrumTeam object to JSON format.
        /// </summary>
        /// <param name="writer">Text writer to serialize the Scrum Team into.</param>
        /// <param name="scrumTeam">The Scrum Team to serialize.</param>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Object should be injected.")]
        public void Serialize(TextWriter writer, ScrumTeam scrumTeam)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (scrumTeam == null)
            {
                throw new ArgumentNullException(nameof(scrumTeam));
            }

            var data = scrumTeam.GetData();
            var serializer = JsonSerializer.Create();
            serializer.ContractResolver = _contractResolver;
            serializer.Serialize(writer, data);
        }

        /// <summary>
        /// Deserializes ScrumTeam object from JSON.
        /// </summary>
        /// <param name="reader">Text reader to deserialize Scrum Team from.</param>
        /// <returns>Deserialized ScrumTeam object.</returns>
        public ScrumTeam Deserialize(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = JsonSerializer.Create();
                serializer.ContractResolver = _contractResolver;
                var data = serializer.Deserialize<ScrumTeamData>(jsonReader);
                return new ScrumTeam(data, _dateTimeProvider);
            }
        }
    }
}
