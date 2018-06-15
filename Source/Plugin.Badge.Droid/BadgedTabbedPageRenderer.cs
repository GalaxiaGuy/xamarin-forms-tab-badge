﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android.AppCompat;
using Xamarin.Forms;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Plugin.Badge.Abstractions;
using Xamarin.Forms.Platform.Android;
using Android.Content;
using Android.Support.Design.Internal;

namespace Plugin.Badge.Droid
{
    public class BadgedTabbedPageRenderer : TabbedPageRenderer
    {
        private const int DeleayBeforeTabAdded = 10;
        protected readonly Dictionary<Element, BadgeView> BadgeViews = new Dictionary<Element, BadgeView>();
        protected bool IsBottomTabPlacement => (Element != null) && Xamarin.Forms.PlatformConfiguration.AndroidSpecific.TabbedPage.GetToolbarPlacement(Element.OnThisPlatform()) == Xamarin.Forms.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom;

        private TabLayout _tabLayout;
        private LinearLayout _tabStrip;

        private BottomNavigationView _bottomNavigationView;

        public BadgedTabbedPageRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<TabbedPage> e)
        {
            base.OnElementChanged(e);

            // make sure we cleanup old event registrations
            Cleanup(e.OldElement);
            Cleanup(Element);

            if (IsBottomTabPlacement)
            {
                _bottomNavigationView = ViewGroup.FindChildOfType<BottomNavigationView>();
                if (_bottomNavigationView == null)
                {
                    Console.WriteLine("Plugin.Badge: No BottomNavigationView found. Badge not added.");
                    return;
                }

                for (var i = 0; i < _bottomNavigationView.Menu.Size(); i++)
                {
                    AddTabBadge(i);
                }
            }
            else
            {
                _tabLayout = ViewGroup.FindChildOfType<TabLayout>();
                if (_tabLayout == null)
                {
                    Console.WriteLine("Plugin.Badge: No TabLayout found. Badge not added.");
                    return;
                }

                _tabStrip = _tabLayout.FindChildOfType<LinearLayout>();

                for (var i = 0; i < _tabLayout.TabCount; i++)
                {
                    AddTabBadge(i);
                }
            }

            Element.ChildAdded += OnTabAdded;
            Element.ChildRemoved += OnTabRemoved;
        }


        private void AddTabBadge(int tabIndex)
        {
            var element = Element.Children[tabIndex];
            BadgeView badgeView = null;
            Android.Views.View view = null;
            if (IsBottomTabPlacement)
            {
                view = ((ViewGroup)_bottomNavigationView?.GetChildAt(0))?.GetChildAt(tabIndex);
            }
            else
            {
                view = (_tabLayout?.GetTabAt(tabIndex).CustomView ?? _tabStrip?.GetChildAt(tabIndex));
            }

            badgeView = (view as ViewGroup)?.FindChildOfType<BadgeView>();

            if (badgeView == null)
            {
                var imageView = (view as ViewGroup)?.FindChildOfType<ImageView>();

                var badgeTarget = imageView?.Drawable != null
                    ? (Android.Views.View)imageView
                    : (view as ViewGroup)?.FindChildOfType<TextView>();

                //create badge for tab
                badgeView = new BadgeView(Context, badgeTarget);
            }

            BadgeViews[element] = badgeView;

            badgeView.UpdateFromElement(element);

            element.PropertyChanged -= OnTabbedPagePropertyChanged;
            element.PropertyChanged += OnTabbedPagePropertyChanged;
        }

        protected virtual void OnTabbedPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!(sender is Element element))
                return;

            if (BadgeViews.TryGetValue(element, out var badgeView))
            {
                badgeView.UpdateFromPropertyChangedEvent(element, e);
            }
        }

        private void OnTabRemoved(object sender, ElementEventArgs e)
        {
            e.Element.PropertyChanged -= OnTabbedPagePropertyChanged;
            BadgeViews.Remove(e.Element);
        }

        private async void OnTabAdded(object sender, ElementEventArgs e)
        {
            await Task.Delay(DeleayBeforeTabAdded);

            if (!(e.Element is Page page))
                return;

            AddTabBadge(Element.Children.IndexOf(page));
        }

        protected override void Dispose(bool disposing)
        {
            Cleanup(Element);

            base.Dispose(disposing);
        }

        private void Cleanup(TabbedPage page)
        {
            if (page == null)
            {
                return;
            }

            foreach (var tab in page.Children)
            {
                tab.PropertyChanged -= OnTabbedPagePropertyChanged;
            }

            page.ChildRemoved -= OnTabRemoved;
            page.ChildAdded -= OnTabAdded;

            BadgeViews.Clear();
            _tabLayout = null;
            _tabStrip = null;
        }
    }
}
