// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";
import markdoc from "@astrojs/markdoc";
import starlightHeadingBadges from "starlight-heading-badges";
import starlightImageZoom from "starlight-image-zoom";

export default defineConfig({
  site: process.env.SITE,
  base: process.env.BASE_URL,
  integrations: [
    markdoc(),
    starlight({
      plugins: [starlightHeadingBadges(), starlightImageZoom()],
      components: {
        MarkdownContent: "./src/components/overrides/MarkdownContent.astro",
        ThemeSelect: "./src/components/overrides/ThemeSelect.astro",
      },
      title: "Heavy Weather",
      description: "Documentation for the Heavy Weather platform.",
      favicon: "/assets/logo-light.svg",
      logo: {
        light: "./public/assets/logo-light.svg",
        dark: "./public/assets/logo-dark.svg",
        replacesTitle: false,
      },
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/MKKL1/weather-station',
        },
      ],
      customCss: ["./src/styles/lexend-fonts.css", "./src/styles/custom.css"],
      sidebar: [
        {
          label: "Getting Started",
          autogenerate: { directory: "getting-started" },
        },
        { label: "Architecture", autogenerate: { directory: "architecture" } },
        {
          label: "API Reference",
          items: [
            { label: "Overview", link: "api/" },
            {
              label: "Main API",
              link: "api-reference/main",
              attrs: { target: "_blank", class: "external-sidebar-link" },
            },
            {
              label: "Gateway API",
              link: "api-reference/gateway",
              attrs: { target: "_blank", class: "external-sidebar-link" },
            },
          ],
        },
        { label: "Guides", autogenerate: { directory: "guides" } },
      ],
      lastUpdated: true,
      tableOfContents: { minHeadingLevel: 2, maxHeadingLevel: 4 },
    }),
  ],
  experimental: {
    svgo: true
  },
});
