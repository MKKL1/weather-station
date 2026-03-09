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
      },
      title: "Heavy Weather",
      description: "Documentation for the Heavy Weather platform.",
      logo: {
        light: "./src/assets/logo-light.svg",
        dark: "./src/assets/logo-dark.svg",
        replacesTitle: false,
      },
      customCss: ["./src/styles/custom.css"],
      sidebar: [
        {
          label: "Getting Started",
          autogenerate: { directory: "getting-started" },
        },
        { label: "Architecture", autogenerate: { directory: "architecture" } },
        {
          label: "API Reference",
          items: [
            { label: "Overview", link: "/api/" },
            {
              label: "Main API",
              link: "/api-reference/main",
              attrs: { target: "_blank", class: "external-sidebar-link" },
            },
            {
              label: "Gateway API",
              link: "/api-reference/gateway",
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
});
