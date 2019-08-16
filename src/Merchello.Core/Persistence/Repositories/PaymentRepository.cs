﻿namespace Merchello.Core.Persistence.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Merchello.Core.Models;
    using Merchello.Core.Models.EntityBase;
    using Merchello.Core.Models.Rdbms;
    using Merchello.Core.Persistence.Factories;
    using Merchello.Core.Persistence.Querying;

    using Umbraco.Core;
    using Umbraco.Core.Cache;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Persistence;
    using Umbraco.Core.Persistence.Querying;
    using Umbraco.Core.Persistence.SqlSyntax;

    using IDatabaseUnitOfWork = Merchello.Core.Persistence.UnitOfWork.IDatabaseUnitOfWork;

    /// <summary>
    /// The payment repository.
    /// </summary>
    internal class PaymentRepository : MerchelloPetaPocoRepositoryBase<IPayment>, IPaymentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentRepository"/> class.
        /// </summary>
        /// <param name="work">
        /// The work.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <param name="sqlSyntax">
        /// The SQL syntax.
        /// </param>
        public PaymentRepository(IDatabaseUnitOfWork work, ILogger logger, ISqlSyntaxProvider sqlSyntax)
            : base(work, logger, sqlSyntax)
        {
        }

        #region Overrides of RepositoryBase<IPayment>

        /// <summary>
        /// Gets a <see cref="IPayment"/> by it's key.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="IPayment"/>.
        /// </returns>
        protected override IPayment PerformGet(Guid key)
        {
            var sql = GetBaseQuery(false)
                .Where(GetBaseWhereClause(), new { Key = key });

            var dto = Database.Fetch<PaymentDto>(sql).FirstOrDefault();

            if (dto == null)
                return null;

            var factory = new PaymentFactory();

            var payment = factory.BuildEntity(dto);

            return payment;
        }

        /// <summary>
        /// Gets all <see cref="IPayment"/>.
        /// </summary>
        /// <param name="keys">
        /// The keys.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IPayment}"/>.
        /// </returns>
        protected override IEnumerable<IPayment> PerformGetAll(params Guid[] keys)
        {

            var dtos = new List<PaymentDto>();

            if (keys.Any())
            {
                // This is to get around the WhereIn max limit of 2100 parameters and to help with performance of each WhereIn query
                var keyLists = keys.Split(400).ToList();

                // Loop the split keys and get them
                foreach (var keyList in keyLists)
                {
                    dtos.AddRange(Database.Fetch<PaymentDto>(GetBaseQuery(false).WhereIn<PaymentDto>(x => x.Key, keyList, SqlSyntax)));
                }
            }
            else
            {
                dtos = Database.Fetch<PaymentDto>(GetBaseQuery(false));
            }

            var factory = new PaymentFactory();
            foreach (var dto in dtos)
            {
                yield return factory.BuildEntity(dto);
            }
        }

        #endregion

        #region Overrides of MerchelloPetaPocoRepositoryBase<IPayment>

        /// <summary>
        /// The get base query.
        /// </summary>
        /// <param name="isCount">
        /// The is count.
        /// </param>
        /// <returns>
        /// The <see cref="Sql"/>.
        /// </returns>
        protected override Sql GetBaseQuery(bool isCount)
        {
            var sql = new Sql();
            sql.Select(isCount ? "COUNT(*)" : "*")
               .From<PaymentDto>(SqlSyntax);

            return sql;
        }

        /// <summary>
        /// Gets the base where clause.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        protected override string GetBaseWhereClause()
        {
            return "merchPayment.pk = @Key";
        }

        /// <summary>
        /// Gets a list delete clauses.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{String}"/>.
        /// </returns>
        protected override IEnumerable<string> GetDeleteClauses()
        {
            var list = new List<string>
                {
                    "DELETE FROM merchAppliedPayment WHERE paymentKey = @Key",
                    "DELETE FROM merchPayment WHERE pk = @Key"
                };

            return list;
        }

        /// <summary>
        /// Saves a new item to the database.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        protected override void PersistNewItem(IPayment entity)
        {
            ((Entity)entity).AddingEntity();

            var factory = new PaymentFactory();
            var dto = factory.BuildDto(entity);

            Database.Insert(dto);
            entity.Key = dto.Key;
            entity.ResetDirtyProperties();
        }

        /// <summary>
        /// Updates an existing item in the database.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        protected override void PersistUpdatedItem(IPayment entity)
        {
            ((Entity)entity).UpdatingEntity();

            var factory = new PaymentFactory();
            var dto = factory.BuildDto(entity);

            Database.Update(dto);

            entity.ResetDirtyProperties();
        }

        /// <summary>
        /// Deletes item from the database.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        protected override void PersistDeletedItem(IPayment entity)
        {
            var deletes = GetDeleteClauses();
            foreach (var delete in deletes)
            {
                Database.Execute(delete, new { entity.Key });
            }
        }

        /// <summary>
        /// Gets a collection of <see cref="IPayment"/> by query.
        /// </summary>
        /// <param name="query">
        /// The query.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IPayment}"/>.
        /// </returns>
        protected override IEnumerable<IPayment> PerformGetByQuery(IQuery<IPayment> query)
        {
            var sqlClause = GetBaseQuery(false);
            var translator = new SqlTranslator<IPayment>(sqlClause, query);
            var sql = translator.Translate();

            var dtos = Database.Fetch<PaymentDto>(sql);

            return dtos.DistinctBy(x => x.Key).Select(dto => Get(dto.Key));
        }

        #endregion
    }
}
