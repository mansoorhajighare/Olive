﻿using System;
using System.Threading.Tasks;
using Olive.Entities;

namespace Olive.Services.Globalization
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the translation of this object's string representation.
        /// </summary>
        public static Task<string> ToString(this IEntity instance, ILanguage language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            return Translator.Translate(instance.ToString(), language);
        }
    }
}
