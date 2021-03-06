﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace LightJson.Serialization
{
    using System.IO;
    using ErrorType = JsonParseException.ErrorType;

    /// <summary>
    /// Represents a text scanner that reads one character at a time.
    /// </summary>
    internal sealed class TextScanner
    {
        private TextReader reader;
        private TextPosition position;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextScanner"/> class.
        /// </summary>
        /// <param name="reader">The TextReader to read the text.</param>
        public TextScanner(TextReader reader)
        {
            this.reader = reader;
        }

        /// <summary>
        /// Gets the position of the scanner within the text.
        /// </summary>
        /// <value>The position of the scanner within the text.</value>
        public TextPosition Position
        {
            get
            {
                return this.position;
            }
        }

        /// <summary>
        /// Reads the next character in the stream without changing the current position.
        /// </summary>
        /// <returns>The next character in the stream.</returns>
        public char Peek()
        {
            var next = this.reader.Peek();

            if (next == -1)
            {
                throw new JsonParseException(
                    ErrorType.IncompleteMessage,
                    this.position);
            }
            else
            {
                return (char)next;
            }
        }

        /// <summary>
        /// Reads the next character in the stream, advancing the text position.
        /// </summary>
        /// <returns>The next character in the stream.</returns>
        public char Read()
        {
            var next = this.reader.Read();

            if (next == -1)
            {
                throw new JsonParseException(
                    ErrorType.IncompleteMessage,
                    this.position);
            }
            else
            {
                if (next == '\n')
                {
                    this.position.Line += 1;
                    this.position.Column = 0;
                }
                else
                {
                    this.position.Column += 1;
                }

                return (char)next;
            }
        }

        /// <summary>
        /// Advances the scanner to next non-whitespace character.
        /// </summary>
        public void SkipWhitespace()
        {
            while (true)
            {
                char next = this.Peek();
                if (char.IsWhiteSpace(next))
                {
                    this.Read();
                    continue;
                }
                else if (next == '/')
                {
                    this.SkipCommentOrInvalidSlash();
                    continue;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Verifies that the given character matches the next character in the stream.
        /// If the characters do not match, an exception will be thrown.
        /// </summary>
        /// <param name="next">The expected character.</param>
        public void Assert(char next)
        {
            if (this.Read() != next)
            {
                throw new JsonParseException(
                    string.Format("Parser expected '{0}'", next),
                    ErrorType.InvalidOrUnexpectedCharacter,
                    this.position);
            }
        }

        /// <summary>
        /// Verifies that the given string matches the next characters in the stream.
        /// If the strings do not match, an exception will be thrown.
        /// </summary>
        /// <param name="next">The expected string.</param>
        public void Assert(string next)
        {
            for (var i = 0; i < next.Length; i += 1)
            {
                this.Assert(next[i]);
            }
        }

        private void SkipCommentOrInvalidSlash()
        {
            // First character is the a slash
            this.Read();
            switch (this.Peek())
            {
            case '/':
                this.SkipLineComment();
                return;

            case '*':
                this.SkipBlockComment();
                return;

            default:
                return;
            }
        }

        private void SkipLineComment()
        {
            // First character is the second '/' of the opening '//'
            this.Read();

            while (true)
            {
                switch (this.reader.Peek())
                {
                case '\n':
                    // Reached the end of the line
                    this.Read();
                    return;

                case -1:
                    // Reached the end of the file
                    return;

                default:
                    this.Read();
                    continue;
                }
            }
        }

        private void SkipBlockComment()
        {
            // First character is the '*' of the opening '/*'
            this.Read();

            bool foundStar = false;
            while (true)
            {
                switch (this.reader.Peek())
                {
                case '*':
                    this.Read();
                    foundStar = true;
                    continue;

                case '/':
                    this.Read();
                    if (foundStar)
                    {
                        return;
                    }
                    else
                    {
                        foundStar = false;
                        continue;
                    }

                case -1:
                    // Reached the end of the file
                    return;

                default:
                    this.Read();
                    foundStar = false;
                    continue;
                }
            }
        }
    }
}
