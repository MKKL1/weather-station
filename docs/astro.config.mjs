// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";
import markdoc from "@astrojs/markdoc";

export default defineConfig({
  integrations: [
    markdoc(),
    starlight({
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
        { label: "API Reference", autogenerate: { directory: "api" } },
        { label: "Guides", autogenerate: { directory: "guides" } },
      ],
      lastUpdated: true,
      tableOfContents: { minHeadingLevel: 2, maxHeadingLevel: 4 },
    }),
  ],
});
