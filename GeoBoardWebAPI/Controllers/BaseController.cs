using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using GeoBoardWebAPI.Attributes;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Extensions.Authorization;
using GeoBoardWebAPI.Models;
using GeoBoardWebAPI.Extensions;

namespace GeoBoardWebAPI.Controllers
{
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public abstract class BaseController : Controller
    {
        protected readonly IMapper _mapper;
        protected readonly IStringLocalizer<Startup> _localizer;
        protected readonly UserRepository UserRepository;

        public BaseController(IServiceProvider scopeFactory)
        {
            _mapper = scopeFactory.GetRequiredService<IMapper>();
            _localizer = scopeFactory.GetRequiredService<IStringLocalizer<Startup>>();
            UserRepository = scopeFactory.GetRequiredService<UserRepository>();
        }

        public T GetFromQueryString<T>(HttpRequest request) where T : new()
        {
            var obj = new T();
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var valueAsString = request.Query[property.Name];
                var value = Parse(property.PropertyType, valueAsString);

                if (value == null)
                    continue;

                property.SetValue(obj, value, null);
            }
            return obj;
        }

        public object Parse(Type dataType, string ValueToConvert)
        {
            TypeConverter obj = TypeDescriptor.GetConverter(dataType);
            object value = obj.ConvertFromString(null, CultureInfo.InvariantCulture, ValueToConvert);
            return value;
        }

        #region ReturnMethods
        protected async virtual Task<OkObjectResult> Ok<TEntity, TViewModel>(
            IQueryable<TEntity> queryable,
            params (Expression<Func<TEntity, object>> orderByKey, System.ComponentModel.ListSortDirection sortDirection)[] defaultOrderBy)
            where TEntity : class
            where TViewModel : class
        {
            var viewModelType = typeof(TViewModel);
            var request = HttpContext.Request;

            var maxItemsPerPage = 1000;

            var defaultItemsPerPage = 20;

            var qPage = request.Query["page"];
            var qItemsPerPage = request.Query["itemsPerPage"];
            string qOrderBy = request.Query["orderBy"];
            string qSearch = request.Query["search"];
            string qFilter = request.Query["filter"]; //json object maken

            if (!int.TryParse(qPage.FirstOrDefault(), out int page) || page < 1)
            {
                page = 1;
            }

            if (!int.TryParse(qItemsPerPage.FirstOrDefault(), out int itemsPerPage) || itemsPerPage > maxItemsPerPage)
            {
                itemsPerPage = defaultItemsPerPage;
            }

            queryable = Search(queryable, viewModelType, qSearch);

            var filterResult = new List<FilterHttpRequestModel>();

            queryable = Filter(queryable, viewModelType, qFilter, filterResult);

            var orderByResult = new List<OrderByHttpRequestModel>();

            queryable = OrderBy(queryable, viewModelType, qOrderBy, orderByResult);

            //Confirm order is present. We need a order by for Skip and Take results.
            if (!orderByResult.Any())
            {
                var defaultOrderByList = new List<string>();

                foreach (var orderByItem in defaultOrderBy)
                {
                    var expressionBody = orderByItem.orderByKey.Body;
                    var memberExpr = expressionBody as MemberExpression;
                    var unaryExpr = expressionBody as UnaryExpression;
                    var memberName = (memberExpr ?? (unaryExpr != null ? unaryExpr.Operand as MemberExpression : null)).Member.Name;

                    var sortOrderString = (orderByItem.sortDirection == System.ComponentModel.ListSortDirection.Descending)
                         ? "DESC"
                         : "ASC";

                    orderByResult.Add(new OrderByHttpRequestModel()
                    {
                        Key = memberName,
                        Direction = sortOrderString
                    });

                    defaultOrderByList.Add($"{memberName} {sortOrderString}");
                }

                qOrderBy = string.Join(", ", defaultOrderByList);
                queryable = queryable.OrderBy(qOrderBy);
            }

            var totalCount = queryable.Count();

            queryable = queryable
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage);

            var configuration = new MapperConfiguration(cfg => {
                cfg.CreateMap<TEntity, TViewModel>();
                cfg.AddProfile(new MappingProfile());
                }) ;

            var items = await queryable.ProjectTo<TViewModel>(configuration).ToListAsync();

            var responseModel = new CollectionHttpResponseModel<TViewModel>()
            {
                Page = page,
                ItemsPerPage = itemsPerPage,
                OrderBy = orderByResult,
                Filter = filterResult,
                TotalCount = totalCount,
                ResultCount = items.Count,
                Search = qSearch,
                Items = items
            };

            return Ok(responseModel);
        }


        [NonAction]
        protected async virtual Task<OkObjectResult> SingularOk<TViewModel>(
            IQueryable<TViewModel> queryable,
            params (Expression<Func<TViewModel, object>> orderByKey, System.ComponentModel.ListSortDirection sortDirection)[] defaultOrderBy)
            where TViewModel : class
        {
            var viewModelType = typeof(TViewModel);
            var request = HttpContext.Request;

            var maxItemsPerPage = 1000;

            var defaultItemsPerPage = 20;

            var qPage = request.Query["page"];
            var qItemsPerPage = request.Query["itemsPerPage"];
            string qOrderBy = request.Query["orderBy"];
            string qSearch = request.Query["search"];
            string qFilter = request.Query["filter"]; //json object maken

            if (!int.TryParse(qPage.FirstOrDefault(), out int page) || page < 1)
            {
                page = 1;
            }

            if (!int.TryParse(qItemsPerPage.FirstOrDefault(), out int itemsPerPage) || itemsPerPage > maxItemsPerPage)
            {
                itemsPerPage = defaultItemsPerPage;
            }

            queryable = Search(queryable, viewModelType, qSearch);

            var filterResult = new List<FilterHttpRequestModel>();

            queryable = Filter(queryable, viewModelType, qFilter, filterResult);

            var orderByResult = new List<OrderByHttpRequestModel>();

            queryable = OrderBy(queryable, viewModelType, qOrderBy, orderByResult);

            //Confirm order is present. We need a order by for Skip and Take results.
            if (!orderByResult.Any())
            {
                var defaultOrderByList = new List<string>();

                foreach (var orderByItem in defaultOrderBy)
                {
                    var expressionBody = orderByItem.orderByKey.Body;
                    var memberExpr = expressionBody as MemberExpression;
                    var unaryExpr = expressionBody as UnaryExpression;
                    var memberName = (memberExpr ?? (unaryExpr != null ? unaryExpr.Operand as MemberExpression : null)).Member.Name;

                    var sortOrderString = (orderByItem.sortDirection == System.ComponentModel.ListSortDirection.Descending)
                         ? "DESC"
                         : "ASC";

                    orderByResult.Add(new OrderByHttpRequestModel()
                    {
                        Key = memberName,
                        Direction = sortOrderString
                    });

                    defaultOrderByList.Add($"{memberName} {sortOrderString}");
                }

                qOrderBy = string.Join(", ", defaultOrderByList);
                queryable = queryable.OrderBy(qOrderBy);
            }

            var totalCount = queryable.Count();

            queryable = queryable
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
            ;

            var items = queryable.ToList();

            var responseModel = new CollectionHttpResponseModel<TViewModel>()
            {
                Page = page,
                ItemsPerPage = itemsPerPage,
                OrderBy = orderByResult,
                Filter = filterResult,
                TotalCount = totalCount,
                ResultCount = items.Count,
                Search = qSearch,
                Items = items
            };

            return Ok(responseModel);
        }

        [NonAction]
        protected async virtual Task<OkObjectResult> SingularOk<TViewModel>(
            IQueryable<TViewModel> queryable, int totalRows = 0,
            params (Expression<Func<TViewModel, object>> orderByKey, System.ComponentModel.ListSortDirection sortDirection)[] defaultOrderBy)
    where TViewModel : class
        {
            var viewModelType = typeof(TViewModel);
            var request = HttpContext.Request;

            var maxItemsPerPage = 1000;

            var defaultItemsPerPage = 20;

            var qPage = request.Query["page"];
            var qItemsPerPage = request.Query["itemsPerPage"];
            string qOrderBy = request.Query["orderBy"];
            string qSearch = request.Query["search"];
            string qFilter = request.Query["filter"]; //json object maken

            if (!int.TryParse(qPage.FirstOrDefault(), out int page) || page < 1)
            {
                page = 1;
            }

            if (!int.TryParse(qItemsPerPage.FirstOrDefault(), out int itemsPerPage) || itemsPerPage > maxItemsPerPage)
            {
                itemsPerPage = defaultItemsPerPage;
            }

            queryable = Search(queryable, viewModelType, qSearch);

            var filterResult = new List<FilterHttpRequestModel>();
            queryable = Filter(queryable, viewModelType, qFilter, filterResult);

            var orderByResult = new List<OrderByHttpRequestModel>();

            queryable = OrderBy(queryable, viewModelType, qOrderBy, orderByResult);

            //Confirm order is present. We need a order by for Skip and Take results.
            if (!orderByResult.Any())
            {
                var defaultOrderByList = new List<string>();

                foreach (var orderByItem in defaultOrderBy)
                {
                    var expressionBody = orderByItem.orderByKey.Body;
                    var memberExpr = expressionBody as MemberExpression;
                    var unaryExpr = expressionBody as UnaryExpression;
                    var memberName = (memberExpr ?? (unaryExpr != null ? unaryExpr.Operand as MemberExpression : null)).Member.Name;

                    var sortOrderString = (orderByItem.sortDirection == System.ComponentModel.ListSortDirection.Descending)
                         ? "DESC"
                         : "ASC";

                    orderByResult.Add(new OrderByHttpRequestModel()
                    {
                        Key = memberName,
                        Direction = sortOrderString
                    });

                    defaultOrderByList.Add($"{memberName} {sortOrderString}");
                }

                qOrderBy = string.Join(", ", defaultOrderByList);
                queryable = queryable.OrderBy(qOrderBy);
            }

            var totalCount = totalRows > 0 ? totalRows : queryable.Count();

            //queryable = queryable
            //    .Skip((page - 1) * itemsPerPage)
            //    .Take(itemsPerPage)
            //;

            var items = queryable.ToList();

            var responseModel = new CollectionHttpResponseModel<TViewModel>()
            {
                Page = page,
                ItemsPerPage = itemsPerPage,
                OrderBy = orderByResult,
                Filter = filterResult,
                TotalCount = totalCount,
                ResultCount = items.Count,
                Search = qSearch,
                Items = items
            };

            return Ok(responseModel);
        }


        [NonAction]
        protected BadRequestObjectResult BadRequest(LocalizedString error)
        {
            var response = new BadRequestHttpResponseModel(error);

            return BadRequest(response);
        }

        [NonAction]
        protected BadRequestObjectResult BadRequest(IEnumerable<LocalizedString> errors)
        {
            var response = new BadRequestHttpResponseModel(errors);

            return BadRequest(response);
        }
        #endregion

        [NonAction]
        public Guid? GetUserId()
        {
            if (null != User && null != User.Identity && User.Identity.IsAuthenticated)
            {
                return new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            return null;
        }

        [NonAction]
        public Dictionary<PropertyInfo, string> GetAllAttributedProps(Type currType, Type attribute, List<Type> recursionPrevention = null)
        {
            if (recursionPrevention == null) recursionPrevention = new List<Type>();

            Dictionary<PropertyInfo, string> props = new Dictionary<PropertyInfo, string>();
            if (recursionPrevention.Contains(currType)) return null;
            recursionPrevention.Add(currType);
            foreach (PropertyInfo pi in currType.GetProperties())
            {
                if (!pi.PropertyType.IsPrimitive && !pi.PropertyType.Assembly.ToString().ToLower().Contains("system"))
                {
                    foreach (var keyvalue in GetAllAttributedProps(pi.PropertyType, attribute, recursionPrevention))
                    {
                        props.Add(keyvalue.Key, pi.Name + (!string.IsNullOrEmpty(keyvalue.Value) ? "." + keyvalue.Value : string.Empty));
                    }
                }
                else if (pi.IsDefined(attribute))
                {
                    props.Add(pi, "");
                }
            }
            return props;
        }

        [NonAction]
        private IQueryable<TEntity> OrderBy<TEntity>(IQueryable<TEntity> queryable, Type viewModelType, string qOrderBy, List<OrderByHttpRequestModel> orderByResult) where TEntity : class
        {
            if (!string.IsNullOrWhiteSpace(qOrderBy))
            {
                var orderByProperties = viewModelType
                    .GetProperties()
                    .Where(prop => prop.IsDefined(typeof(OrderableAttribute), false))
                ;

                if (orderByProperties.Any())
                {
                    var qOrderByCollection = JsonConvert.DeserializeObject<List<OrderByHttpRequestModel>>(qOrderBy);

                    foreach (var orderBy in qOrderByCollection)
                    {
                        orderBy.Direction = (orderBy.Direction ?? "ASC").ToUpper();

                        if (string.IsNullOrWhiteSpace(orderBy.Key))
                        {
                            continue;
                        }

                        var orderByField = orderByProperties.FirstOrDefault(x => x.Name.ToLower().Equals(orderBy.Key.ToLower()));
                        if (null == orderByField)
                        {
                            continue;
                        }

                        if (!orderBy.Direction.Equals("ASC") && !orderBy.Direction.Equals("DESC"))
                        {
                            orderBy.Direction = "ASC";
                        }

                        orderByResult.Add(orderBy);
                    }

                    if (orderByResult.Any())
                    {
                        queryable = queryable.OrderBy(string.Join(", ", orderByResult.Select(x => $"{x.Key} {x.Direction}")));
                    }
                }
            }

            return queryable;
        }

        [NonAction]
        private IQueryable<TEntity> Search<TEntity>(IQueryable<TEntity> queryable, Type viewModelType, string qSearch) where TEntity : class
        {
            if (!string.IsNullOrWhiteSpace(qSearch))
            {
                var searchProperties = GetAllAttributedProps(viewModelType, typeof(SearchableAttribute));
                if (searchProperties.Any())
                {
                    var searchQueryParts = new List<string>();
                    var searchForQueryPartArgs = new List<object>();

                    var i = 0;
                    foreach (var searchProperty in searchProperties)
                    {
                        var prep = "";
                        if (!string.IsNullOrEmpty(searchProperty.Value))
                        {
                            prep += searchProperty.Value + ".";
                        }

                        switch (searchProperty.Key.PropertyType.ToString())
                        {
                            case "System.Nullable`1[System.Int32]":
                            case "System.Int32":
                                searchQueryParts.Add($"{prep}{searchProperty.Key.Name} == @{i}");
                                break;


                            case "System.String":
                            default:
                                searchQueryParts.Add($"{prep}{searchProperty.Key.Name}.Contains(@{i})");
                                break;
                        }
                        searchForQueryPartArgs.Add(qSearch.ToString());
                        i++;
                    }

                    var searchQuery = string.Join(" OR ", searchQueryParts);
                    queryable = queryable.Where(searchQuery, searchForQueryPartArgs.ToArray());
                }
            }

            return queryable;
        }

        [NonAction]
        private IQueryable<TViewModel> Filter<TViewModel>(IQueryable<TViewModel> queryable, Type viewModelType, string qFilter, List<FilterHttpRequestModel> filterResult) where TViewModel : class
        {
            if (!string.IsNullOrWhiteSpace(qFilter))
            {
                var filterProperties = viewModelType
                    .GetProperties()
                    .Where(prop => prop.IsDefined(typeof(FilterableAttribute), false))
                ;

                if (filterProperties.Any())
                {
                    var qFilterCollection = JsonConvert.DeserializeObject<List<FilterHttpRequestModel>>(qFilter);

                    bool isFilterValid(FilterHttpRequestModel filter)
                    {
                        if (string.IsNullOrWhiteSpace(filter.Key)
                            || string.IsNullOrWhiteSpace(filter.Operator)
                        )
                        {
                            return false;
                        }

                        var supportedOperators = new string[] {
                            "==",
                            "!=",
                            ">",
                            ">=",
                            "<",
                            "<=",
                            "IS",
                            "IS NOT",
                        };

                        if (!supportedOperators.Contains(filter.Operator))
                        {
                            return false;
                        }

                        var filterField = filterProperties.FirstOrDefault(x => x.Name.ToLower().Equals(filter.Key.ToLower()));
                        if (null == filterField)
                        {
                            return false;
                        }

                        return true;
                    };

                    Expression<Func<TViewModel, bool>> getFilterLambda(FilterHttpRequestModel filter)
                    {
                        if (!isFilterValid(filter))
                        {
                            throw new Exception("Invalid filter found");
                        }

                        var selector = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8);
                        var parameter = Expression.Parameter(typeof(TViewModel), selector);
                        var member = Expression.Property(parameter, filter.Key);
                        var memberTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(member.Type);
                        var constant = Expression.Constant(memberTypeConverter.ConvertFrom(filter.Value), member.Type);
                        BinaryExpression body = null;

                        switch (filter.Operator)
                        {
                            case "==":
                                {
                                    body = Expression.Equal(member, constant);
                                    break;
                                }
                            case "!=":
                                {
                                    body = Expression.NotEqual(member, constant);
                                    break;
                                }
                            case ">":
                                {
                                    body = Expression.GreaterThan(member, constant);
                                    break;
                                }
                            case ">=":
                                {
                                    body = Expression.GreaterThanOrEqual(member, constant);
                                    break;
                                }
                            case "<":
                                {
                                    body = Expression.LessThan(member, constant);
                                    break;
                                }
                            case "<=":
                                {
                                    body = Expression.LessThanOrEqual(member, constant);
                                    break;
                                }
                        }

                        if (filter.Ands != null)
                        {
                            foreach (var andFilter in filter.Ands)
                            {
                                var andFilterLambda = getFilterLambda(andFilter);
                                body = Expression.AndAlso(body, andFilterLambda.Body);
                            }
                        }

                        if (filter.Ors != null)
                        {
                            foreach (var andFilter in filter.Ors)
                            {
                                var andFilterLambda = getFilterLambda(andFilter);
                                body = Expression.OrElse(body, andFilterLambda.Body);
                            }
                        }

                        return Expression.Lambda<Func<TViewModel, bool>>(body, parameter);
                    }

                    foreach (var filter in qFilterCollection)
                    {
                        var filterLambda = getFilterLambda(filter);
                        queryable = queryable.Where(filterLambda);

                        filterResult.Add(filter);
                    }
                }
            }

            return queryable;
        }
    }
}
