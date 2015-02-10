﻿// <copyright>
// Copyright (c) 2012 Rasto Novotny
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duracellko.PlanningPoker.Domain
{
    /// <summary>
    /// Generic message that can be sent to Scrum team members or observers.
    /// </summary>
    [Serializable]
    public class Message
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="type">The message type.</param>
        public Message(MessageType type)
        {
            this.MessageType = type;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of the message.
        /// </summary>
        /// <value>
        /// The type of the message.
        /// </value>
        public MessageType MessageType { get; private set; }

        /// <summary>
        /// Gets the message ID unique to member, so that member can track which messages he/she already got.
        /// </summary>
        /// <value>The message ID.</value>
        public long Id { get; internal set; }

        #endregion
    }
}