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

namespace Basilisque.DataAccess.EntityFramework.Base.SoftDelete;

/// <summary>
/// Explicitly permits physical deletion in the current asynchronous control flow.
/// </summary>
public sealed class SoftDeleteSuppressor : IDisposable
{
    private static readonly AsyncLocal<bool> _isHardDeleteAllowed = new();
    private readonly bool _previousValue;
    private bool _isDisposed;

    private SoftDeleteSuppressor(bool previousValue)
    {
        _previousValue = previousValue;
    }

    /// <summary>
    /// Gets whether physical deletion is currently permitted for soft-deletable entities.
    /// </summary>
    public static bool IsHardDeleteAllowed => _isHardDeleteAllowed.Value;

    /// <summary>
    /// Allows physical deletion until the returned scope is disposed.
    /// </summary>
    public static IDisposable AllowHardDelete()
    {
        var previousValue = _isHardDeleteAllowed.Value;
        _isHardDeleteAllowed.Value = true;
        return new SoftDeleteSuppressor(previousValue);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isHardDeleteAllowed.Value = _previousValue;
        _isDisposed = true;
    }
}
