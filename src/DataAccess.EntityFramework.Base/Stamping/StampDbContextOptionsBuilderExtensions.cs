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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.DataAccess.EntityFramework.Base.Stamping;

/// <summary>
/// Provides database-context options configuration for entity stamping.
/// </summary>
public static class StampDbContextOptionsBuilderExtensions
{
    extension(DbContextOptionsBuilder optionsBuilder)
    {
        /// <summary>
        /// Adds the registered entity stamping interceptor to the database context options.
        /// </summary>
        /// <param name="serviceProvider">The scoped service provider used to resolve the interceptor.</param>
        /// <returns>The options builder.</returns>
        public DbContextOptionsBuilder UseEFCoreStamping(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            return optionsBuilder.UseEFCoreStamping(serviceProvider.GetRequiredService<CompositeStampInterceptor>());
        }

        /// <summary>
        /// Adds the registered entity stamping interceptor to the database context options.
        /// </summary>
        /// <param name="compositeStampInterceptor">The composite stamp interceptor to add.</param>
        /// <returns>The options builder.</returns>
        internal DbContextOptionsBuilder UseEFCoreStamping(CompositeStampInterceptor compositeStampInterceptor)
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);
            ArgumentNullException.ThrowIfNull(compositeStampInterceptor);

            return optionsBuilder.AddInterceptors(compositeStampInterceptor);
        }
    }
}
