/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Temporarily suppresses entity stamping for the current asynchronous control flow.
/// </summary>
public sealed class StampSuppressor : IDisposable
{
    private static readonly AsyncLocal<bool> _isSuppressed = new();
    private readonly bool _wasSuppressed;
    private bool _isDisposed;

    private StampSuppressor(bool wasSuppressed)
    {
        _wasSuppressed = wasSuppressed;
    }

    /// <summary>
    /// Gets whether entity stamping is currently suppressed.
    /// </summary>
    public static bool IsSuppressed => _isSuppressed.Value;

    /// <summary>
    /// Suppresses entity stamping until the returned scope is disposed.
    /// </summary>
    /// <returns>A scope that restores the previous suppression state when disposed.</returns>
    public static IDisposable Suppress()
    {
        var previousValue = _isSuppressed.Value;
        _isSuppressed.Value = true;

        return new StampSuppressor(previousValue);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isSuppressed.Value = _wasSuppressed;
        _isDisposed = true;
    }
}
