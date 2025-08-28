// ====================================================================================================
// <copyright file="ObservableCollectionExtensions.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Klasa zawierająca metody rozszerzające dla <see cref="ObservableCollection{T}"/>.
/// </summary>
public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Sortuje kolekcję ObservableCollection w miejscu, minimalizując liczbę powiadomień do interfejsu użytkownika.
    /// </summary>
    /// <typeparam name="T">Typ elementów w kolekcji.</typeparam>
    /// <param name="collection">Kolekcja do posortowania.</param>
    /// <param name="comparer">Komparator do porównywania elementów.</param>
    public static void Sort<T>(this ObservableCollection<T> collection, IComparer<T> comparer)
    {
        List<T> sorted = [.. collection.OrderBy(x => x, comparer)];

        int ptr = 0;
        while (ptr < sorted.Count)
        {
            // Jeśli element jest już na właściwym miejscu, przejdź do następnego.
            if (comparer.Compare(collection[ptr], sorted[ptr]) == 0)
            {
                ptr++;
            }
            else // W przeciwnym razie znajdź właściwy element i przenieś go na bieżącą pozycję.
            {
                T itemToMove = sorted[ptr];
                int oldIndex = collection.IndexOf(itemToMove);

                if (oldIndex >= 0)
                {
                    collection.Move(oldIndex, ptr);
                }

                ptr++;
            }
        }
    }
}