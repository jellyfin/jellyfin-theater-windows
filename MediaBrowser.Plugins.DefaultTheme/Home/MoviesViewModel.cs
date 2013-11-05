﻿using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Plugins.DefaultTheme.ListPage;
using MediaBrowser.Theater.Interfaces.Navigation;
using MediaBrowser.Theater.Interfaces.Playback;
using MediaBrowser.Theater.Interfaces.Presentation;
using MediaBrowser.Theater.Interfaces.Session;
using MediaBrowser.Theater.Interfaces.ViewModels;
using MediaBrowser.Theater.Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MediaBrowser.Plugins.DefaultTheme.Home
{
    public class MoviesViewModel : BaseHomePageSectionViewModel, IDisposable, IHasActivePresentation
    {
        private readonly ISessionManager _sessionManager;
        private readonly IImageManager _imageManager;
        private readonly INavigationService _navService;
        private readonly IPlaybackManager _playbackManager;
        private readonly ILogger _logger;
        private readonly IServerEvents _serverEvents;

        public ItemListViewModel LatestTrailersViewModel { get; private set; }
        public ItemListViewModel LatestMoviesViewModel { get; private set; }
        public ItemListViewModel MiniSpotlightsViewModel { get; private set; }
        public ItemListViewModel MiniSpotlightsViewModel2 { get; private set; }

        public ImageViewerViewModel SpotlightViewModel { get; private set; }

        public GalleryViewModel GenresViewModel { get; private set; }
        public GalleryViewModel AllMoviesViewModel { get; private set; }
        public GalleryViewModel ActorsViewModel { get; private set; }
        public GalleryViewModel BoxsetsViewModel { get; private set; }
        public GalleryViewModel TrailersViewModel { get; private set; }
        public GalleryViewModel HDMoviesViewModel { get; private set; }
        public GalleryViewModel ThreeDMoviesViewModel { get; private set; }
        public GalleryViewModel FamilyMoviesViewModel { get; private set; }
        public GalleryViewModel ComedyItemsViewModel { get; private set; }
        public GalleryViewModel RomanticMoviesViewModel { get; private set; }
        public GalleryViewModel YearsViewModel { get; private set; }

        private readonly double _posterTileHeight;
        private readonly double _posterTileWidth;

        private MoviesView _moviesView;

        public MoviesViewModel(IPresentationManager presentation, IImageManager imageManager, IApiClient apiClient, ISessionManager session, INavigationService nav, IPlaybackManager playback, ILogger logger, double tileWidth, double tileHeight, IServerEvents serverEvents)
            : base(presentation, apiClient)
        {
            _sessionManager = session;
            _imageManager = imageManager;
            _navService = nav;
            _playbackManager = playback;
            _logger = logger;
            _serverEvents = serverEvents;

            TileWidth = tileWidth;
            TileHeight = tileHeight;

            _posterTileHeight = (TileHeight * 1.46) + TilePadding / 2;
            _posterTileWidth = _posterTileHeight * 2 / 3;

            const double tileScaleFactor = 13;

            ActorsViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToActorsInternal)
            };

            GenresViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToGenresInternal)
            };

            YearsViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToYearsInternal)
            };

            AllMoviesViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToAllMoviesInternal)
            };

            BoxsetsViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToBoxsetsInternal)
            };

            TrailersViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToTrailersInternal)
            };

            HDMoviesViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToHDMoviesInternal)
            };

            FamilyMoviesViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToFamilyMoviesInternal)
            };

            ThreeDMoviesViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateTo3DMoviesInternal)
            };

            RomanticMoviesViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToRomanticMoviesInternal)
            };

            ComedyItemsViewModel = new GalleryViewModel(ApiClient, _imageManager, _navService)
            {
                GalleryHeight = TileHeight,
                GalleryWidth = TileWidth * tileScaleFactor / 16,
                CustomCommandAction = () => NavigateWithLoading(NavigateToComedyMoviesInternal)
            };

            var spotlightTileWidth = TileWidth * 2 + TilePadding;
            var spotlightTileHeight = spotlightTileWidth * 9 / 16;

            SpotlightViewModel = new ImageViewerViewModel(_imageManager, new List<ImageViewerImage>())
            {
                Height = spotlightTileHeight,
                Width = spotlightTileWidth,
                CustomCommandAction = i => _navService.NavigateToItem(i.Item, ViewType.Movies),
                ImageStretch = Stretch.UniformToFill
            };

            LoadViewModels();
        }

        public const int PosterWidth = 214;
        public const int ThumbstripWidth = 600;
        public const int ListImageWidth = 160;
        public const int PosterStripWidth = 290;

        public static void SetDefaults(ListPageConfig config)
        {
            config.DefaultViewType = ListViewTypes.Poster;
            config.PosterImageWidth = PosterWidth;
            config.ThumbImageWidth = ThumbstripWidth;
            config.ListImageWidth = ListImageWidth;
            config.PosterStripImageWidth = PosterStripWidth;
        }

        private async void LoadViewModels()
        {
            PresentationManager.ShowLoadingAnimation();

            var cancellationSource = _mainViewCancellationTokenSource = new CancellationTokenSource();

            try
            {
                var view = await ApiClient.GetMovieView(_sessionManager.CurrentUser.Id, cancellationSource.Token);

                _moviesView = view;

                LoadSpotlightViewModel(view);
                LoadBoxsetsViewModel(view);
                LoadTrailersViewModel(view);
                LoadAllMoviesViewModel(view);
                LoadHDMoviesViewModel(view);
                LoadFamilyMoviesViewModel(view);
                Load3DMoviesViewModel(view);
                LoadComedyMoviesViewModel(view);
                LoadRomanticMoviesViewModel(view);
                LoadActorsViewModel(view);
                LoadMiniSpotlightsViewModel(view);
                LoadMiniSpotlightsViewModel2(view);
                LoadLatestMoviesViewModel(view);
                LoadLatestTrailersViewModel(view);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting movie view", ex);
                PresentationManager.ShowDefaultErrorMessage();
            }
            finally
            {
                PresentationManager.HideLoadingAnimation();
                DisposeMainViewCancellationTokenSource(false);
            }
        }

        private void LoadLatestMoviesViewModel(MoviesView view)
        {
            LatestMoviesViewModel = new ItemListViewModel(GetLatestMoviesAsync, PresentationManager, _imageManager, ApiClient, _navService, _playbackManager, _logger, _serverEvents)
            {
                ImageDisplayWidth = _posterTileWidth,
                ImageDisplayHeightGenerator = v => _posterTileHeight,
                DisplayNameGenerator = HomePageViewModel.GetDisplayName,
                PreferredImageTypesGenerator = vm => new[] { ImageType.Primary, ImageType.Backdrop, ImageType.Thumb, },
                EnableBackdropsForCurrentItem = false,
                ListType = "LatestMovies"
            };

            OnPropertyChanged("LatestMoviesViewModel");

            ShowLatestMovies = view.LatestMovies.Count > 0;
        }

        private void LoadLatestTrailersViewModel(MoviesView view)
        {
            LatestTrailersViewModel = new ItemListViewModel(GetLatestTrailersAsync, PresentationManager, _imageManager, ApiClient, _navService, _playbackManager, _logger, _serverEvents)
            {
                ImageDisplayWidth = _posterTileWidth,
                ImageDisplayHeightGenerator = v => _posterTileHeight,
                DisplayNameGenerator = HomePageViewModel.GetDisplayName,
                PreferredImageTypesGenerator = vm => new[] { ImageType.Primary },
                EnableBackdropsForCurrentItem = false
            };

            OnPropertyChanged("LatestTrailersViewModel");

            ShowLatestTrailers = view.LatestTrailers.Count > 0;
        }

        private Task<ItemsResult> GetLatestTrailersAsync(ItemListViewModel viewModel)
        {
            var result = new ItemsResult
            {
                Items = _moviesView.LatestTrailers.ToArray(),
                TotalRecordCount = _moviesView.LatestTrailers.Count
            };

            return Task.FromResult(result);
        }

        private Task<ItemsResult> GetLatestMoviesAsync(ItemListViewModel viewModel)
        {
            var result = new ItemsResult
            {
                Items = _moviesView.LatestMovies.ToArray(),
                TotalRecordCount = _moviesView.LatestMovies.Count
            };

            return Task.FromResult(result);
        }

        private void LoadMiniSpotlightsViewModel(MoviesView view)
        {
            Func<ItemListViewModel, Task<ItemsResult>> getItems = vm =>
            {
                var items = view.MiniSpotlights.Take(2).ToArray();

                return Task.FromResult(new ItemsResult
                {
                    TotalRecordCount = items.Length,
                    Items = items
                });
            };

            MiniSpotlightsViewModel = new ItemListViewModel(getItems, PresentationManager, _imageManager, ApiClient, _navService, _playbackManager, _logger, _serverEvents)
            {
                ImageDisplayWidth = TileWidth + (TilePadding / 4) - 1,
                ImageDisplayHeightGenerator = v => TileHeight,
                DisplayNameGenerator = HomePageViewModel.GetDisplayName,
                EnableBackdropsForCurrentItem = false,
                ImageStretch = Stretch.UniformToFill,
                PreferredImageTypesGenerator = vm => new[] { ImageType.Backdrop },
                DownloadImageAtExactSize = true
            };

            OnPropertyChanged("MiniSpotlightsViewModel");
        }

        private void LoadMiniSpotlightsViewModel2(MoviesView view)
        {
            Func<ItemListViewModel, Task<ItemsResult>> getItems = vm =>
            {
                var items = view.MiniSpotlights.Skip(2).Take(3).ToArray();

                return Task.FromResult(new ItemsResult
                {
                    TotalRecordCount = items.Length,
                    Items = items
                });
            };

            MiniSpotlightsViewModel2 = new ItemListViewModel(getItems, PresentationManager, _imageManager, ApiClient, _navService, _playbackManager, _logger, _serverEvents)
            {
                ImageDisplayWidth = TileWidth,
                ImageDisplayHeightGenerator = v => TileHeight,
                DisplayNameGenerator = HomePageViewModel.GetDisplayName,
                EnableBackdropsForCurrentItem = false,
                ImageStretch = Stretch.UniformToFill,
                PreferredImageTypesGenerator = vm => new[] { ImageType.Backdrop },
                DownloadImageAtExactSize = true
            };

            OnPropertyChanged("MiniSpotlightsViewModel2");
        }

        private bool _showLatestMovies;
        public bool ShowLatestMovies
        {
            get { return _showLatestMovies; }

            set
            {
                var changed = _showLatestMovies != value;

                _showLatestMovies = value;

                if (changed)
                {
                    OnPropertyChanged("ShowLatestMovies");
                }
            }
        }

        private bool _showLatestTrailers;
        public bool ShowLatestTrailers
        {
            get { return _showLatestTrailers; }

            set
            {
                var changed = _showLatestTrailers != value;

                _showLatestTrailers = value;

                if (changed)
                {
                    OnPropertyChanged("ShowLatestTrailers");
                }
            }
        }

        private bool _showTrailers;
        public bool ShowTrailers
        {
            get { return _showTrailers; }

            set
            {
                var changed = _showTrailers != value;

                _showTrailers = value;

                if (changed)
                {
                    OnPropertyChanged("ShowTrailers");
                }
            }
        }

        private bool _showBoxSets;
        public bool ShowBoxSets
        {
            get { return _showBoxSets; }

            set
            {
                var changed = _showBoxSets != value;

                _showBoxSets = value;

                if (changed)
                {
                    OnPropertyChanged("ShowBoxSets");
                }
            }
        }

        private bool _show3DMovies;
        public bool Show3DMovies
        {
            get { return _show3DMovies; }

            set
            {
                var changed = _show3DMovies != value;

                _show3DMovies = value;

                if (changed)
                {
                    OnPropertyChanged("Show3DMovies");
                }
            }
        }

        private bool _showRomanticMovies;
        public bool ShowRomanticMovies
        {
            get { return _showRomanticMovies; }

            set
            {
                var changed = _showRomanticMovies != value;

                _showRomanticMovies = value;

                if (changed)
                {
                    OnPropertyChanged("ShowRomanticMovies");
                }
            }
        }

        private bool _showComedyItems;
        public bool ShowComedyItems
        {
            get { return _showComedyItems; }

            set
            {
                var changed = _showComedyItems != value;

                _showComedyItems = value;

                if (changed)
                {
                    OnPropertyChanged("ShowComedyItems");
                }
            }
        }

        private bool _showHdMovies;
        public bool ShowHDMovies
        {
            get { return _showHdMovies; }

            set
            {
                var changed = _showHdMovies != value;

                _showHdMovies = value;

                if (changed)
                {
                    OnPropertyChanged("ShowHDMovies");
                }
            }
        }

        private bool _showFamilyMovies;
        public bool ShowFamilyMovies
        {
            get { return _showFamilyMovies; }

            set
            {
                var changed = _showFamilyMovies != value;

                _showFamilyMovies = value;

                if (changed)
                {
                    OnPropertyChanged("ShowFamilyMovies");
                }
            }
        }

        private void LoadSpotlightViewModel(MoviesView view)
        {
            const ImageType imageType = ImageType.Backdrop;

            var tileWidth = TileWidth * 2 + TilePadding;
            var tileHeight = tileWidth * 9 / 16;

            BackdropItems = view.BackdropItems.ToArray();

            var images = view.SpotlightItems.Select(i => new ImageViewerImage
            {
                Url = ApiClient.GetImageUrl(i, new ImageOptions
                {
                    Height = Convert.ToInt32(tileHeight),
                    Width = Convert.ToInt32(tileWidth),
                    ImageType = imageType

                }),

                Caption = i.Name,
                Item = i

            }).ToList();

            SpotlightViewModel.Images.AddRange(images);
            SpotlightViewModel.StartRotating(10000);
        }

        public void EnableActivePresentation()
        {
            SpotlightViewModel.StartRotating(10000);
        }
        public void DisableActivePresentation()
        {
            SpotlightViewModel.StopRotating();
        }

        private void LoadActorsViewModel(MoviesView view)
        {
            var images = view.PeopleItems.Take(1).Select(i => ApiClient.GetPersonImageUrl(i.Name, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Height = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            ActorsViewModel.AddImages(images);
        }

        private async Task NavigateToGenresInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("MovieGenres", CancellationToken.None);

            var genres = await ApiClient.GetGenresAsync(new ItemsByNameQuery
            {
                IncludeItemTypes = new[] { "Movie" },
                SortBy = new[] { ItemSortBy.SortName },
                Recursive = true,
                UserId = _sessionManager.CurrentUser.Id
            });

            var indexOptions = genres.Items.Select(i => new TabItem
            {
                Name = i.Name,
                DisplayName = i.Name + " (" + i.MovieCount + ")"
            });

            var options = new ListPageConfig
            {
                IndexOptions = indexOptions.ToList(),
                PageTitle = "Movies",
                CustomItemQuery = GetMoviesByGenre
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private async Task NavigateToActorsInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("People", CancellationToken.None);

            var options = new ListPageConfig
            {
                IndexOptions = AlphabetIndex,
                PageTitle = "Movies | People",
                CustomItemQuery = GetAllActors
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private async Task NavigateToYearsInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("MovieYears", CancellationToken.None);

            var yearIndex = await ApiClient.GetYearIndex(_sessionManager.CurrentUser.Id, new[] { "Movie" }, CancellationToken.None);

            var indexOptions = yearIndex.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => new TabItem
            {
                Name = i.Name,
                DisplayName = i.Name + " (" + i.ItemCount + ")"
            });

            var options = new ListPageConfig
            {
                IndexOptions = indexOptions.ToList(),
                PageTitle = "Movies",
                CustomItemQuery = GetMoviesByYear
            };

            SetDefaults(options);

            options.DefaultViewType = ListViewTypes.PosterStrip;

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private void LoadAllMoviesViewModel(MoviesView view)
        {
            var images = view.MovieItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            AllMoviesViewModel.AddImages(images);
        }

        private void LoadHDMoviesViewModel(MoviesView view)
        {
            ShowHDMovies = view.HDItems.Count > 0 && view.HDMoviePercentage > 10 && view.HDMoviePercentage < 90;

            var images = view.HDItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            HDMoviesViewModel.AddImages(images);
        }

        private void LoadRomanticMoviesViewModel(MoviesView view)
        {
            var now = DateTime.Now;

            if (now.DayOfWeek == DayOfWeek.Friday)
            {
                ShowRomanticMovies = view.RomanceItems.Count > 0 && now.Hour >= 15;
            }
            else if (now.DayOfWeek == DayOfWeek.Saturday)
            {
                ShowRomanticMovies = view.RomanceItems.Count > 0 && (now.Hour < 3 || now.Hour >= 15);
            }
            else if (now.DayOfWeek == DayOfWeek.Sunday)
            {
                ShowRomanticMovies = view.RomanceItems.Count > 0 && now.Hour < 3;
            }
            else
            {
                ShowRomanticMovies = false;
            }

            var images = view.RomanceItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            RomanticMoviesViewModel.AddImages(images);
        }

        private void LoadComedyMoviesViewModel(MoviesView view)
        {
            var now = DateTime.Now;

            if (now.DayOfWeek == DayOfWeek.Thursday)
            {
                ShowComedyItems = view.ComedyItems.Count > 0 && now.Hour >= 12;
                ComedyItemsViewModel.Name = "Comedy Night";
            }
            else if (now.DayOfWeek == DayOfWeek.Sunday)
            {
                ShowComedyItems = view.ComedyItems.Count > 0;
                ComedyItemsViewModel.Name = "Sunday Funnies";
            }
            else
            {
                ShowComedyItems = false;
            }

            var images = view.ComedyItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            ComedyItemsViewModel.AddImages(images);
        }

        private void LoadFamilyMoviesViewModel(MoviesView view)
        {
            ShowFamilyMovies = view.FamilyMovies.Count > 0 && view.FamilyMoviePercentage > 10 && view.FamilyMoviePercentage < 90;

            var images = view.FamilyMovies.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            FamilyMoviesViewModel.AddImages(images);
        }

        private void Load3DMoviesViewModel(MoviesView view)
        {
            Show3DMovies = view.ThreeDItems.Count > 0;

            var images = view.ThreeDItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            ThreeDMoviesViewModel.AddImages(images);
        }

        private void LoadBoxsetsViewModel(MoviesView view)
        {
            ShowBoxSets = view.BoxSetItems.Count > 0;

            var images = view.BoxSetItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            BoxsetsViewModel.AddImages(images);
        }

        private void LoadTrailersViewModel(MoviesView view)
        {
            ShowTrailers = view.TrailerItems.Count > 0;

            var images = view.TrailerItems.Take(1).Select(i => ApiClient.GetImageUrl(i.Id, new ImageOptions
            {
                ImageType = i.ImageType,
                Tag = i.ImageTag,
                Width = Convert.ToInt32(TileWidth * 2),
                EnableImageEnhancers = false
            }));

            TrailersViewModel.AddImages(images);
        }

        private async Task NavigateToTrailersInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("Trailers", CancellationToken.None);

            var options = new ListPageConfig
            {
                SortOptions = GetMovieSortOptions(),
                PageTitle = "Trailers",
                CustomItemQuery = GetTrailers
            };

            SetDefaults(options);

            options.DefaultViewType = ListViewTypes.PosterStrip;

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> GetTrailers(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Trailer" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private async Task NavigateToBoxsetsInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("Boxsets", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "Box Sets",
                CustomItemQuery = GetBoxSets
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> GetBoxSets(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "BoxSet" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private async Task NavigateToAllMoviesInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("Movies", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "Movies",
                CustomItemQuery = GetAllMovies,
                SortOptions = GetMovieSortOptions()
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private async Task NavigateToRomanticMoviesInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("RomanticMovies", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "Date Night",
                CustomItemQuery = GetRomanticMovies,
                SortOptions = GetMovieSortOptions()
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> GetRomanticMovies(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                Genres = new[] { ApiClientExtensions.RomanceGenre },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private async Task NavigateToComedyMoviesInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("ComedyMovies", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "Comedy Night",
                CustomItemQuery = GetComedyMovies,
                SortOptions = GetMovieSortOptions()
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> GetComedyMovies(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                Genres = new[] { ApiClientExtensions.ComedyGenre },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private Task<ItemsResult> GetAllMovies(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private Task<ItemsResult> GetMoviesByGenre(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            var indexOption = viewModel.CurrentIndexOption;

            if (indexOption != null)
            {
                query.Genres = new[] { indexOption.Name };
            }

            return ApiClient.GetItemsAsync(query);
        }

        private Task<ItemsResult> GetMoviesByYear(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            var indexOption = viewModel.CurrentIndexOption;

            if (indexOption != null)
            {
                query.Years = new[] { int.Parse(indexOption.Name) };
            }

            return ApiClient.GetItemsAsync(query);
        }

        private async Task NavigateToHDMoviesInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("HDMovies", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "HD Movies",
                CustomItemQuery = GetHDMovies,
                SortOptions = GetMovieSortOptions()
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> GetHDMovies(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                IsHD = true,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private async Task NavigateToFamilyMoviesInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("FamilyMovies", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "Family Movies",
                CustomItemQuery = GetFamilyMovies,
                SortOptions = GetMovieSortOptions()
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> GetFamilyMovies(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                Genres = new[] { ApiClientExtensions.FamilyGenre },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private async Task NavigateTo3DMoviesInternal()
        {
            var item = await GetRootFolder();

            var displayPreferences = await PresentationManager.GetDisplayPreferences("3DMovies", CancellationToken.None);

            var options = new ListPageConfig
            {
                PageTitle = "3D Movies",
                CustomItemQuery = Get3DMovies,
                SortOptions = GetMovieSortOptions()
            };

            SetDefaults(options);

            var page = new FolderPage(item, displayPreferences, ApiClient, _imageManager, PresentationManager, _navService, _playbackManager, _logger, _serverEvents, options)
            {
                ViewType = ViewType.Movies
            };

            await _navService.Navigate(page);
        }

        private Task<ItemsResult> Get3DMovies(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var query = new ItemQuery
            {
                Fields = FolderPage.QueryFields,

                UserId = _sessionManager.CurrentUser.Id,

                IncludeItemTypes = new[] { "Movie" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                Recursive = true,

                Is3D = true
            };

            return ApiClient.GetItemsAsync(query);
        }

        private Task<ItemsResult> GetAllActors(ItemListViewModel viewModel, DisplayPreferences displayPreferences)
        {
            var fields = FolderPage.QueryFields.ToList();
            fields.Remove(ItemFields.Overview);
            fields.Remove(ItemFields.DisplayPreferencesId);
            fields.Remove(ItemFields.DateCreated);

            var query = new PersonsQuery
            {
                Fields = fields.ToArray(),

                IncludeItemTypes = new[] { "Movie", "Trailer" },

                SortBy = !String.IsNullOrEmpty(displayPreferences.SortBy)
                             ? new[] { displayPreferences.SortBy }
                             : new[] { ItemSortBy.SortName },

                SortOrder = displayPreferences.SortOrder,

                UserId = _sessionManager.CurrentUser.Id,

                ImageTypes = new[] { ImageType.Primary },

                Recursive = true
            };

            var indexOption = viewModel.CurrentIndexOption;

            if (indexOption != null)
            {
                if (string.Equals(indexOption.Name, "#", StringComparison.OrdinalIgnoreCase))
                {
                    query.NameLessThan = "A";
                }
                else
                {
                    query.NameStartsWithOrGreater = indexOption.Name;
                    query.NameLessThan = indexOption.Name + "zz";
                }
            }

            return ApiClient.GetPeopleAsync(query);
        }

        internal static Dictionary<string, string> GetMovieSortOptions()
        {
            var sortOptions = new Dictionary<string, string>();
            sortOptions["Name"] = ItemSortBy.SortName;

            sortOptions["Critic Rating"] = ItemSortBy.CriticRating;
            sortOptions["Date Added"] = ItemSortBy.DateCreated;
            sortOptions["IMDb Rating"] = ItemSortBy.CommunityRating;
            sortOptions["Parental Rating"] = ItemSortBy.OfficialRating;
            sortOptions["Release Date"] = ItemSortBy.PremiereDate;
            sortOptions["Runtime"] = ItemSortBy.Runtime;

            return sortOptions;
        }

        private CancellationTokenSource _mainViewCancellationTokenSource;
        private void DisposeMainViewCancellationTokenSource(bool cancel)
        {
            if (_mainViewCancellationTokenSource != null)
            {
                if (cancel)
                {
                    try
                    {
                        _mainViewCancellationTokenSource.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                }
                _mainViewCancellationTokenSource.Dispose();
                _mainViewCancellationTokenSource = null;
            }
        }

        public void Dispose()
        {
            if (LatestTrailersViewModel != null)
            {
                LatestTrailersViewModel.Dispose();
            }
            if (LatestMoviesViewModel != null)
            {
                LatestMoviesViewModel.Dispose();
            }
            if (MiniSpotlightsViewModel != null)
            {
                MiniSpotlightsViewModel.Dispose();
            }
            if (MiniSpotlightsViewModel2 != null)
            {
                MiniSpotlightsViewModel2.Dispose();
            }
            if (SpotlightViewModel != null)
            {
                SpotlightViewModel.Dispose();
            }
            if (GenresViewModel != null)
            {
                GenresViewModel.Dispose();
            }
            if (AllMoviesViewModel != null)
            {
                AllMoviesViewModel.Dispose();
            }
            if (ActorsViewModel != null)
            {
                ActorsViewModel.Dispose();
            }
            if (BoxsetsViewModel != null)
            {
                BoxsetsViewModel.Dispose();
            }
            if (TrailersViewModel != null)
            {
                TrailersViewModel.Dispose();
            }
            if (HDMoviesViewModel != null)
            {
                HDMoviesViewModel.Dispose();
            }
            if (ThreeDMoviesViewModel != null)
            {
                ThreeDMoviesViewModel.Dispose();
            }
            if (FamilyMoviesViewModel != null)
            {
                FamilyMoviesViewModel.Dispose();
            }
            if (ComedyItemsViewModel != null)
            {
                ComedyItemsViewModel.Dispose();
            }
            if (RomanticMoviesViewModel != null)
            {
                RomanticMoviesViewModel.Dispose();
            }
            if (YearsViewModel != null)
            {
                YearsViewModel.Dispose();
            }
            DisposeMainViewCancellationTokenSource(true);
        }
    }
}
